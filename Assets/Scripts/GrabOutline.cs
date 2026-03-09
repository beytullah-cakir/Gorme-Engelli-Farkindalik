using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Herhangi bir XRGrabInteractable objesine ekle.
/// - Hover (algılama) anında outline eklenir.
/// - Tutulunca da outline gösterilir.
/// - Birden fazla hover eventi olsa bile materyal yalnızca bir kez eklenir.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class GrabOutline : MonoBehaviour
{
    [Tooltip("Unity'de kendin oluşturduğun Outline Material'ı buraya sürükle")]
    public Material outlineMaterial;

    [Tooltip("Outline uygulanacak Renderer. Boş bırakılırsa bu objedeki ilk Renderer kullanılır.")]
    public Renderer targetRenderer;

    [Header("Hover UI")]
    [Tooltip("Sepet algılandığında aktif olacak UI (GameObject)")]
    [SerializeField] private GameObject hoverUI1;

    private XRGrabInteractable grab;
    private Material[] originalMaterials;

    // Kaç interactor aktif (hover veya grab) — 0 olunca outline kaldırılır
    private int activeCount = 0;
    private bool isHovered = false;
    // Bir kez tutulduktan sonra UI bir daha gösterilmez
    private bool isGrabbed = false;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();

        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        grab.hoverEntered.AddListener(OnHoverEnter);
        grab.hoverExited.AddListener(OnHoverExit);
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    void OnDestroy()
    {
        grab.hoverEntered.RemoveListener(OnHoverEnter);
        grab.hoverExited.RemoveListener(OnHoverExit);
        grab.selectEntered.RemoveListener(OnGrab);
        grab.selectExited.RemoveListener(OnRelease);
    }

    // ── Hover ──────────────────────────────────────
    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        isHovered = true;
        activeCount++;
        if (activeCount == 1) AddOutline();
        if (!isGrabbed) SetHoverUI(true);
    }

    private void OnHoverExit(HoverExitEventArgs args)
    {
        isHovered = false;
        activeCount = Mathf.Max(0, activeCount - 1);
        if (activeCount == 0) RemoveOutline();
        SetHoverUI(false);
    }

    // ── Grab ───────────────────────────────────────
    private void OnGrab(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        SetHoverUI(false); // Tutununca UI kapanır, bir daha açılmaz
        activeCount++;
        if (activeCount == 1) AddOutline();
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        activeCount = Mathf.Max(0, activeCount - 1);
        if (activeCount == 0) RemoveOutline();
    }

    // ── Yardımcı ───────────────────────────────────
    private void AddOutline()
    {
        if (targetRenderer == null || outlineMaterial == null) return;
        if (originalMaterials != null) return; // Zaten eklendi

        originalMaterials = targetRenderer.materials;  // Runtime kopya al

        var newMats = new Material[originalMaterials.Length + 1];
        originalMaterials.CopyTo(newMats, 0);
        newMats[originalMaterials.Length] = outlineMaterial;

        targetRenderer.materials = newMats;
    }

    private void SetHoverUI(bool state)
    {
        if (hoverUI1 != null) hoverUI1.SetActive(state);
    }

    private void RemoveOutline()
    {
        if (targetRenderer == null || originalMaterials == null) return;

        targetRenderer.materials = originalMaterials;
        originalMaterials = null;
    }
}
