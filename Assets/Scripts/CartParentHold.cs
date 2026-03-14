using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;

public class CartParentHold : MonoBehaviour
{
    public Transform xrOrigin;
    public Vector3 positionOffset = Vector3.zero;

    [Tooltip("TRUE → Sahne 2: Grab sonrası child kalır, hover olmadığında MeshRenderer kapanır.\n" +
             "FALSE → Sahne 1: Child kalmaz, MeshRenderer her zaman açık.")]
    [SerializeField] private bool stayAsChild = false;

    [Header("Hover UI")]
    [Tooltip("Sepet algılandığında aktif olacak UI (GameObject)")]
    [SerializeField] private GameObject hoverUI1;

    private XRGrabInteractable grab;
    private Rigidbody rb;
    public BoxCollider boxCollider; // Dışarıdan atanabilmesi için public yapıldı
    private MeshRenderer meshRenderer;
    private Transform originalParent;
    private bool isHovered = false;

    // Cart ilk kez tutulduğunda Mission 1'in sadece bir kez tetiklenmesini sağlar
    private bool missionCompleted = false;
    // Bir kez tutulduktan sonra UI bir daha gösterilmez
    private bool isGrabbed = false;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        
        // Eğer Inspector'dan atanmamışsa otomatik bulmaya çalış
        if (boxCollider == null)
            boxCollider = GetComponent<BoxCollider>();

        meshRenderer = GetComponentInChildren<MeshRenderer>();
        originalParent = transform.parent;

        grab.hoverEntered.AddListener(OnHoverEnter);
        grab.hoverExited.AddListener(OnHoverExit);
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);

        // stayAsChild=false (Sahne 1): MeshRenderer baştan açık
        if (!stayAsChild && meshRenderer != null)
            meshRenderer.enabled = true;
    }

    void OnHoverEnter(HoverEnterEventArgs args)
    {
        isHovered = true;
        if (meshRenderer != null && !meshRenderer.enabled)
            meshRenderer.enabled = true;
        if (!isGrabbed) SetHoverUI(true);
    }

    void OnHoverExit(HoverExitEventArgs args)
    {
        isHovered = false;
        SetHoverUI(false);

        // stayAsChild=true (Sahne 2): tutulmamışsa MeshRenderer kapat
        // stayAsChild=false (Sahne 1): MeshRenderer her zaman açık kalır
        if (stayAsChild && !isGrabbed && meshRenderer != null)
            meshRenderer.enabled = false;
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        // stayAsChild Inspector'da zaten ayarlı — OnGrab içinde değiştirilmez
        isGrabbed = true;
        SetHoverUI(false);

        if (meshRenderer != null && !meshRenderer.enabled)
            meshRenderer.enabled = true;

        // Tutulduğunda collider'ı kapat (fizik çakışmalarını önlemek için)
        if (boxCollider != null)
            boxCollider.enabled = false;

        // XRGrabInteractable VelocityTracking modunda bile useGravity'yi kapatır — hemen geri aç
        rb.useGravity = true;
        rb.isKinematic = true;

        transform.SetParent(xrOrigin, true);

        // Yalnızca Y eksenini karakterin yönüyle eşitle (X ve Z korunur)
        transform.eulerAngles = new Vector3(
            transform.eulerAngles.x,
            xrOrigin.eulerAngles.y,
            transform.eulerAngles.z
        );

        transform.localPosition = positionOffset;

        // --- GÖREV 1 TAMAMLAMA ---
        if (!missionCompleted)
        {
            missionCompleted = true;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CompleteMission(1);

                // Sepet tutulunca Move Object'i aktif et
                GameObject move = GameManager.Instance.MoveObject;
                if (move != null)
                {
                    move.SetActive(true);
                    Debug.Log("[CartParentHold] MoveObject aktif edildi.");
                }
            }
            else
            {
                Debug.LogWarning("CartParentHold: Sahnede GameManager bulunamadı!");
            }
        }
    }
    void OnRelease(SelectExitEventArgs args)
    {
        // Tutulan obje bırakıldı; UI artık hiç gösterilmez
        SetHoverUI(false);

        if (stayAsChild)
        {
            // Sahne 2: Bırakılsa bile karakterle hareket eder, collider kapalı kalmalı
            transform.SetParent(xrOrigin, true);
            transform.localPosition = positionOffset;
            rb.isKinematic = true;
            rb.useGravity = false;

            if (boxCollider != null) boxCollider.enabled = false;
        }
        else
        {
            // Sahne 1: Normal bırakma, collider tekrar açılır
            transform.SetParent(originalParent);
            rb.isKinematic = false;
            rb.useGravity = true;

            if (boxCollider != null) boxCollider.enabled = true;
        }
    }

    void SetHoverUI(bool state)
    {
        if (hoverUI1 != null) hoverUI1.SetActive(state);
       
    }
}