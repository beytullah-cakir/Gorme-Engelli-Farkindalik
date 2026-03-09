using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

/// <summary>
/// Sahnede sırayla aktif olan birden fazla SplineContainer ile çalışır.
/// Her karede, listedeki aktif (activeInHierarchy) SplineContainer içindeki
/// tüm Spline'lar arasında karaktere en yakın noktayı bulur.
/// Baston yola değmiyorsa ve yatay mesafe eşiği aşılmışsa yönlendirme oku gösterilir.
///
/// KURULUM:
/// 1. Bu script herhangi bir boş GameObject'e eklenir (örn. "PathIndicatorManager").
/// 2. Inspector → "Tüm Spline Container'lar" listesine sahnedeki
///    bütün SplineContainer objelerini sürükle-bırak yapın.
///    (Aktif/Pasif durumları otomatik algılanır.)
/// 3. Diğer alanları doldurun.
/// </summary>
public class SplinePathIndicator : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector Alanları
    // ─────────────────────────────────────────────

    [Header("Spline Listesi (Tümünü Ekle)")]
    [Tooltip("Sahnedeki tüm SplineContainer'ları buraya sürükleyin. " +
             "Hangisi aktifse o kullanılır.")]
    [SerializeField] private SplineContainer[] splineContainers;

    [Header("Karakter ve Takipçi")]
    [Tooltip("Karakterin Transform'u (XR Rig / XR Origin)")]
    [SerializeField] private Transform character;

    [Tooltip("Yolun en yakın noktasına ışınlanan görünmez takipçi obje")]
    [SerializeField] private Transform ghostTrigger;

    [Header("Yönlendirme Oku")]
    [Tooltip("Yön oku objesi (3D ok veya UI Canvas)")]
    [SerializeField] private Transform directionArrow;

    [Tooltip("Okun aktifleşmesi için gereken minimum yatay mesafe (metre)")]
    [SerializeField] private float distanceThreshold = 0.5f;

    [Header("Baston Temas Kaynağı")]
    [Tooltip("Bastonun yola değip değmediğini bilen StickHaptic bileşeni")]
    [SerializeField] private StickHaptic stickHaptic;

    [Header("Ses Uyardı")]
    [Tooltip("Uyarı sesini çalacak AudioSource — clip'i AudioSource üzerinden ayarlayın")]
    [SerializeField] private AudioSource warningAudioSource;

    // ─────────────────────────────────────────────
    //  Dahili Durum
    // ─────────────────────────────────────────────

    /// <summary>Bastonun yol ile temas durumu</summary>
    private bool isTouching;

    /// <summary>Ghost Trigger'ın anlık dünya konumu</summary>
    private Vector3 ghostWorldPos;

    /// <summary>Geçen seferde uyarı şartı sağlanmış mıydı? (kenar algılama)</summary>
    private bool wasWarning = false;

    /// <summary>En yakın noktanın bulunduğu container — Gizmo ve debug için</summary>
    private SplineContainer nearestContainer;

    // ─────────────────────────────────────────────
    //  Awake
    // ─────────────────────────────────────────────
    private void Awake()
    {
        ValidateReferences();

        if (directionArrow != null)
            directionArrow.gameObject.SetActive(false);

        // AudioSource yoksa objenin üzerinden almayı dene
        if (warningAudioSource == null)
            warningAudioSource = GetComponent<AudioSource>();
    }

    private void ValidateReferences()
    {
        if (splineContainers == null || splineContainers.Length == 0)
            Debug.LogError("[SplinePathIndicator] Spline Container listesi boş!", this);
        if (character == null)
            Debug.LogError("[SplinePathIndicator] Character Transform atanmamış!", this);
        if (ghostTrigger == null)
            Debug.LogError("[SplinePathIndicator] Ghost Trigger atanmamış!", this);
        if (directionArrow == null)
            Debug.LogWarning("[SplinePathIndicator] Direction Arrow atanmamış — ok gösterilmeyecek.", this);
        if (stickHaptic == null)
            Debug.LogWarning("[SplinePathIndicator] StickHaptic atanmamış — isTouching her zaman false.", this);
    }

    // ─────────────────────────────────────────────
    //  Ana Döngü
    // ─────────────────────────────────────────────
    private void Update()
    {
        if (character == null || ghostTrigger == null) return;

        // 1) Tüm aktif container'lar + spline'lar içinde en yakın noktayı bul
        bool hasAny = UpdateGhostTrigger();

        // Hiç aktif container yoksa oku gizle ve çık
        if (!hasAny)
        {
            SetArrowVisible(false);
            StopWarningSound();
            return;
        }

        // 2) Temas durumunu güncelle
        UpdateTouchingState();

        // 3) Yönlendirme okunu ve ses uyarısını güncelle
        UpdateDirectionArrow();
    }

    // ─────────────────────────────────────────────
    //  1 — Ghost Trigger Güncelleme
    //      Tüm aktif container'lar ve içlerindeki
    //      tüm Spline'lar arasında global en yakın
    //      noktayı bulur.
    // ─────────────────────────────────────────────
    /// <returns>En az bir aktif spline bulunduysa true</returns>
    private bool UpdateGhostTrigger()
    {
        if (splineContainers == null || splineContainers.Length == 0)
            return false;

        float   bestSqrDistWorld = float.MaxValue;
        Vector3 bestWorldPoint   = Vector3.zero;
        nearestContainer         = null;

        foreach (var root in splineContainers)
        {
            // Null güvenliği + hiyerarşide aktif mi?
            if (root == null || !root.gameObject.activeInHierarchy)
                continue;

            // Parent'ın kendi SplineContainer'ı + tüm child SplineContainer'ları
            // (includeInactive: false → sadece aktif olan child'lar)
            SplineContainer[] candidates =
                root.GetComponentsInChildren<SplineContainer>(false);

            foreach (var container in candidates)
            {
                // Child obje pasifse atla
                if (!container.gameObject.activeInHierarchy)
                    continue;

                if (container.Splines == null || container.Splines.Count == 0)
                    continue;

                // Karakteri bu container'ın yerel uzayına çevir
                float3 localCharPos = container.transform
                    .InverseTransformPoint(character.position);

                for (int i = 0; i < container.Splines.Count; i++)
                {
                    SplineUtility.GetNearestPoint(
                        container.Splines[i],
                        localCharPos,
                        out float3 nearestLocal,
                        out float  t
                    );

                    // Dünya uzayına çevir (scale farkları için TransformPoint kullan)
                    Vector3 nearestWorld = container.transform.TransformPoint(nearestLocal);

                    // Dünya uzayında XZ mesafesinin karesi (Y farkını dışla)
                    float dx = nearestWorld.x - character.position.x;
                    float dz = nearestWorld.z - character.position.z;
                    float sqrDist = dx * dx + dz * dz;

                    if (sqrDist < bestSqrDistWorld)
                    {
                        bestSqrDistWorld = sqrDist;
                        bestWorldPoint   = nearestWorld;
                        nearestContainer = container; // child container kaydedilir
                    }
                }
            }
        }

        if (nearestContainer == null)
            return false;

        // Ghost Trigger: X-Z yatay düzlem, Y = karakter yüksekliği
        ghostWorldPos = new Vector3(
            bestWorldPoint.x,
            character.position.y,
            bestWorldPoint.z
        );

        ghostTrigger.position = ghostWorldPos;
        return true;
    }

    // ─────────────────────────────────────────────
    //  3 — Temas Durumu
    // ─────────────────────────────────────────────
    private void UpdateTouchingState()
    {
        isTouching = stickHaptic != null && stickHaptic.IsTouchingPath;
    }

    // ─────────────────────────────────────────────
    //  4 — Yönlendirme Oku
    // ─────────────────────────────────────────────
    private void UpdateDirectionArrow()
    {
        if (directionArrow == null) return;

        // Sadece X-Z ekseninde yatay mesafe
        float horizontalDist = Vector2.Distance(
            new Vector2(character.position.x,  character.position.z),
            new Vector2(ghostWorldPos.x,        ghostWorldPos.z)
        );

        bool shouldWarn = !isTouching && horizontalDist > distanceThreshold;

        SetArrowVisible(shouldWarn);
        UpdateWarningSound(shouldWarn);

        if (!shouldWarn) return;

        // Yatay düzlemde bak — Y farkını sıfırla
        Vector3 lookTarget = new Vector3(ghostWorldPos.x,
                                         directionArrow.position.y,
                                         ghostWorldPos.z);

        Vector3 dir = lookTarget - directionArrow.position;

        if (dir.sqrMagnitude > 0.001f)
            directionArrow.rotation = Quaternion.LookRotation(dir);
    }

    // ─────────────────────────────────────────────
    //  5 — Ses Uyardı Sistemi
    // ─────────────────────────────────────────────
    private void UpdateWarningSound(bool shouldWarn)
    {
        if (warningAudioSource == null) return;

        // Yoldan her uzaklaşıldığında (pozitif kenar) sesi çal
        bool justEntered = shouldWarn && !wasWarning;
        if (justEntered)
        {
            warningAudioSource.Play(); // Clip AudioSource'dan okunur
        }

        // Yola geri dönüldüğünde (negatif kenar) ses durdurulur
        bool justLeft = !shouldWarn && wasWarning;
        if (justLeft && warningAudioSource.isPlaying)
        {
            warningAudioSource.Stop();
        }

        wasWarning = shouldWarn;
    }

    private void StopWarningSound()
    {
        if (warningAudioSource != null && warningAudioSource.isPlaying)
            warningAudioSource.Stop();

        wasWarning = false;
    }

    // ─────────────────────────────────────────────
    //  Yardımcı — Ok Görünürlüğü
    // ─────────────────────────────────────────────
    private void SetArrowVisible(bool visible)
    {
        if (directionArrow == null) return;
        if (directionArrow.gameObject.activeSelf != visible)
            directionArrow.gameObject.SetActive(visible);
    }

    // ─────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────

    /// <summary>StickHaptic kullanılmıyorsa manuel temas bildirimi.</summary>
    public void SetTouching(bool touching) => isTouching = touching;

    // ─────────────────────────────────────────────
    //  Gizmo — Editor görsel yardımı
    // ─────────────────────────────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (character == null || ghostTrigger == null) return;

        // Karakter → Ghost Trigger çizgisi
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(character.position, ghostTrigger.position);

        // Ghost Trigger küresi
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(ghostTrigger.position, 0.1f);

        // En yakın container etiketi
        if (nearestContainer != null)
        {
            UnityEditor.Handles.Label(
                nearestContainer.transform.position + Vector3.up * 0.5f,
                $"[En Yakın Spline]\n{nearestContainer.gameObject.name}");
        }

        // Threshold çemberi
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
        DrawCircleGizmo(character.position, distanceThreshold, 32);
    }

    private void DrawCircleGizmo(Vector3 center, float radius, int segments)
    {
        float step = 360f / segments;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float a = i * step * Mathf.Deg2Rad;
            Vector3 next = center + new Vector3(Mathf.Cos(a) * radius, 0f,
                                                Mathf.Sin(a) * radius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
#endif
}
