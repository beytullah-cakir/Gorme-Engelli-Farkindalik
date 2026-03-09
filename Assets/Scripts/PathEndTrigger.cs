using UnityEngine;

/// <summary>
/// Her yolun sonundaki Trigger (Collider) objesine eklenir.
///
/// DAVRANIM:
/// • Karakter bu alana girdiğinde → GameManager.MoveObject deaktif olur + ses çalar.
/// • Inspector'da belirtilen görev (missionToWatch) tamamlandığında
///   GameManager.OnMissionCompleted eventi dinlenir:
///     - MoveObject tekrar aktif olur.
///     - Eğer stopOnMission aynı görevse çalan ses durdurulur.
///
/// KURULUM:
/// 1. Yolun sonuna boş bir GameObject ekle, Is Trigger ✓ olan bir Collider ver.
/// 2. Bu scripti o objeye ekle.
/// 3. Inspector → Mission To Watch: bu yolun hangi göreve karşılık geldiğini yaz.
/// 4. Inspector → Clip: trigger'a girilince çalacak ses klibini bağla.
/// 5. Move Object artık GameManager Inspector'ından atanmıştır — burada alan yok.
/// </summary>
public class PathEndTrigger : MonoBehaviour
{

    // ─────────────────────────────────────────────
    //  Inspector Alanları — Ses
    // ─────────────────────────────────────────────

    [Header("Ses Ayarları")]
    [Tooltip("Alana girildiğinde çalacak ses klibi")]
    [SerializeField] private AudioClip clip;

    [Tooltip("Ses seviyesi (0-1)")]
    [SerializeField][Range(0f, 1f)] private float volume = 1f;

    [Tooltip("Ses sadece bir kez mi çalsın?")]
    [SerializeField] private bool playOnce = true;

    // ─────────────────────────────────────────────
    //  Inspector Alanları — Görev Bağlantısı
    // ─────────────────────────────────────────────

    [Header("Görev Bağlantısı")]
    [Tooltip("Hangi görev tamamlandığında moveObject tekrar AKTİF olsun? " +
             "(GameManager.CompleteMission() çağrısındaki index ile eşleşmeli)")]
    [SerializeField] private int missionToWatch = 1;

    [Tooltip("Hangi görev tamamlandığında ses durdurulsun? " +
             "(0 = hiç durdurma — genellikle missionToWatch ile aynı yapılır)")]
    [SerializeField] private int stopOnMissionIndex = 0;

    // ─────────────────────────────────────────────
    //  Inspector Alanları — Karakter
    // ─────────────────────────────────────────────

    [Header("Karakter Etiketi")]
    [Tooltip("Trigger'a girecek karakterin tag'i")]
    [SerializeField] private string characterTag = "Player";

    // ─────────────────────────────────────────────
    //  Dahili Durum
    // ─────────────────────────────────────────────

    /// <summary>Karakter bu trigger'a en az bir kez girdi mi?</summary>
    private bool triggered = false;

    /// <summary>Ses için otomatik oluşturulan AudioSource</summary>
    private AudioSource audioSource;

    /// <summary>Ses zaten çaldı mı? (playOnce için)</summary>
    private bool hasPlayed = false;

    // ─────────────────────────────────────────────
    //  Yaşam Döngüsü
    // ─────────────────────────────────────────────

    private void Awake()
    {
        // AudioSource'u runtime'da ekle ve ayarla
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip         = clip;
        audioSource.volume       = volume;
        audioSource.playOnAwake  = false;
        audioSource.spatialBlend = 1f; // 3D ses
    }

    private void OnEnable()
    {
        GameManager.OnMissionCompleted += OnMissionCompleted;
    }

    private void OnDisable()
    {
        GameManager.OnMissionCompleted -= OnMissionCompleted;
    }

    // ─────────────────────────────────────────────
    //  Trigger
    // ─────────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(characterTag)) return;

        // ── Move Object deaktif et ──────────────────
        if (!triggered)
        {
            triggered = true;

            GameObject move = GameManager.Instance != null ? GameManager.Instance.MoveObject : null;
            if (move != null)
            {
                move.SetActive(false);
                Debug.Log($"[PathEndTrigger] '{move.name}' deaktif edildi. " +
                          $"(Görev {missionToWatch} tamamlanınca tekrar açılacak)");
            }
            else
            {
                Debug.LogWarning("[PathEndTrigger] GameManager.MoveObject atanmamış!", this);
            }
        }

        // ── Ses çal ─────────────────────────────────
        if (playOnce && hasPlayed) return;
        if (clip == null) return;

        audioSource.Play();
        hasPlayed = true;
        Debug.Log($"[PathEndTrigger] Ses çalındı: {clip.name}");
    }

    // ─────────────────────────────────────────────
    //  Görev Tamamlandı Callback
    // ─────────────────────────────────────────────

    private void OnMissionCompleted(int completedMission)
    {
        // ── Move Object'i geri aç ───────────────────
        if (completedMission == missionToWatch)
        {
            GameObject move = GameManager.Instance != null ? GameManager.Instance.MoveObject : null;
            if (move != null)
            {
                move.SetActive(true);
                Debug.Log($"[PathEndTrigger] Görev {missionToWatch} tamamlandı → " +
                          $"'{move.name}' tekrar aktif edildi.");
            }

            // Trigger'ı sıfırla — sonraki yolda tekrar tetiklenebilsin
            triggered = false;
        }

        // ── Sesi durdur ──────────────────────────────
        if (stopOnMissionIndex != 0 && completedMission == stopOnMissionIndex)
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
                Debug.Log($"[PathEndTrigger] Görev {completedMission} tamamlandı, ses durduruldu.");
            }
        }
    }
}
