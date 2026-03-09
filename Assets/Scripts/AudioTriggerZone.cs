using UnityEngine;

public class AudioTriggerZone : MonoBehaviour
{
    [Header("Ses Ayarları")]
    [Tooltip("Alana girildiğinde çalacak ses klibi")]
    [SerializeField] private AudioClip clip;

    [Tooltip("Ses seviyesi (0-1)")]
    [SerializeField][Range(0f, 1f)] private float volume = 1f;

    [Header("Görev Bağlantısı")]
    [Tooltip("Hangi görev tamamlandığında bu ses durdurulsun? (0 = durdurma)")]
    [SerializeField] private int stopOnMissionIndex = 0;

    [Header("Davranış")]
    [Tooltip("Ses sadece bir kez mi çalsın?")]
    [SerializeField] private bool playOnce = true;

    private AudioSource audioSource;
    private bool hasPlayed = false;

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.playOnAwake = false;
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

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (playOnce && hasPlayed) return;
        if (clip == null) return;

        audioSource.Play();
        hasPlayed = true;

        Debug.Log($"[AudioTriggerZone] Ses çalındı: {clip.name}");
    }

    private void OnMissionCompleted(int missionIndex)
    {
        if (stopOnMissionIndex == 0) return;
        if (missionIndex != stopOnMissionIndex) return;

        if (audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log($"[AudioTriggerZone] Görev {missionIndex} tamamlandı, ses durduruldu.");
        }
    }
}
