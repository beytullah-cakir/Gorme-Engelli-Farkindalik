using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // Görev tamamlandığında tüm dinleyicilere bildirim gönderir
    public static event System.Action<int> OnMissionCompleted;

    [Header("UI Ayarları")]
    [Tooltip("Ekranda görevleri gösterecek olan TextMeshPro Text objesi")]
    [SerializeField] private TMP_Text missionText;

    [Header("Başlangıç Senaryosu (Narrator)")]
    [Tooltip("Narrator UI objesi — narrator başlınca açılır, bitince kapanır")]
    [SerializeField] private GameObject narratorUIObject;
    [Tooltip("Narrator bittikten sonra AKTİF olacak obje (Move objesi)")]
    [SerializeField] private GameObject moveObject;

    /// <summary>Diğer scriptlerin erişebileceği merkezi Move Object referansı.</summary>
    public GameObject MoveObject => moveObject;

    [Tooltip("Narrator sesinin çalacağı AudioSource")]
    [SerializeField] private AudioSource narratorAudioSource;

    [Tooltip("Ekstra bekleme süresi (sn). AudioSource varsa bu yoksayılır.")]
    [SerializeField] private float overrideWaitTime = 0f;

    // Narrator atlama flag'leri
    private bool narratorSkipped = false;
    private bool isNarratorActive = false;

    [Header("Tutorial")]
    [Tooltip("Hareket tutorial objesi — narrator bitince açılır")]
    [SerializeField] private GameObject moveTutorialObj;
    [Tooltip("Dönme tutorial objesi — oyuncu hareket edince açılır")]
    [SerializeField] private GameObject rotateTutorialObj;
    [Tooltip("Move + Turn tutoriali bittikten sonra AKTİF olacak obje — baston yola ilk değdiğinde DESTROY edilir")]
    [SerializeField] private GameObject afterTutorialObject;

    /// <summary>StickHaptic'in ilk Path temasında yok edeceği obje (afterTutorialObject ile aynı).</summary>
    public GameObject PathFirstTouchDestroyObject => afterTutorialObject;
    [Tooltip("Hareket için joystick eşiği (varsayılan 0.3)")]
    [SerializeField] private float tutorialMoveThreshold = 0.3f;
    [Tooltip("Dönme için joystick eşiği (varsayılan 0.3)")]
    [SerializeField] private float tutorialTurnThreshold = 0.3f;

    // Tutorial durum flag'leri
    private bool tutorialActive   = false; // Narrator bitti mi?
    private bool moveDone         = false; // Oyuncu hareket etti mi?
    private bool turnDone         = false; // Oyuncu döndü mü?

    [Header("Ses Efektleri (SFX)")]
    [Tooltip("Tüm görevler tamamlandığında çalacak ortak Başarı Sesi")]
    [SerializeField] private AudioClip missionSuccessSound;
    [Tooltip("Başarı sesinin çalacağı AudioSource")]
    [SerializeField] private AudioSource sfxAudioSource;

    [Header("Görev Açıklamaları")]
    [Tooltip("Narrator bittikten sonra gösterilecek ilk görev metni")]
    [SerializeField] private string mission1Text = "Görev: Alışveriş yapmak için Market Arabasını bul ve al!";
    [Tooltip("Görev 1 tamamlanınca gösterilecek metin")]
    [SerializeField] private string mission2Text = "Görev: Reyondan 1. ürünü al ve sepete koy";
    [Tooltip("Görev 2 tamamlanınca gösterilecek metin")]
    [SerializeField] private string mission3Text = "Görev: Dondurucudan 2. ürünü al ve sepete koy";
    [Tooltip("Görev 3 tamamlanınca gösterilecek metin")]
    [SerializeField] private string mission4Text = "Görev: Reyondan 3. ürünü al ve sepete koy";
    [Tooltip("Görev 4 tamamlanınca gösterilecek metin")]
    [SerializeField] private string mission5Text = "Görev: Bütün alışveriş tamamlandı! Kasaya ilerle.";
    [Tooltip("Görev 5 tamamlanınca gösterilecek metin")]
    [SerializeField] private string missionEndText = "Tebrikler! Alışverişi başarıyla tamamladın!";

    [Header("Görev 1 (Cart Aktivasyonu)")]
    [SerializeField] private GameObject mission1Object;
    [Tooltip("Görev 1 tamamlanınca konuşacak Narrator Sesi (İsteğe bağlı)")]
    [SerializeField] private AudioClip mission1NarratorSpeech;

    [Header("Görev 2")]
    [SerializeField] private GameObject mission2Object;
    [Tooltip("Görev 2 tamamlanınca konuşacak Narrator Sesi (İsteğe bağlı)")]
    [SerializeField] private AudioClip mission2NarratorSpeech;

    [Header("Görev 3")]
    [SerializeField] private GameObject mission3Object;
    [Tooltip("Görev 3 tamamlanınca konuşacak Narrator Sesi (İsteğe bağlı)")]
    [SerializeField] private AudioClip mission3NarratorSpeech;

    [Header("Görev 4")]
    [SerializeField] private GameObject mission4Object;
    [Tooltip("Görev 4 tamamlanınca konuşacak Narrator Sesi (İsteğe bağlı)")]
    [SerializeField] private AudioClip mission4NarratorSpeech;

    [Header("Görev 5 (Final)")]
    [Tooltip("Kasa trigger alanı objesi — atanmazsa (null) sorun olmaz")]
    [SerializeField] private GameObject mission5TriggerObject;
    [Tooltip("Path objesi — atanmazsa (null) sorun olmaz")]
    [SerializeField] private GameObject mission5PathObject;
    [Tooltip("Görev 5 (Oyun Sonu) tamamlanınca konuşacak Narrator Sesi (İsteğe bağlı)")]
    [SerializeField] private AudioClip mission5NarratorSpeech;
    [Tooltip("Görev 5 tamamlanınca AKTİF olacak objeler listesi")]
    [SerializeField] private GameObject[] mission5ObjectsToActivate;
    [Tooltip("Görev 5 tamamlanınca DEAKTİF olacak objeler listesi")]
    [SerializeField] private GameObject[] mission5ObjectsToDeactivate;

    [Header("Final Ekranı")]
    [Tooltip("Tüm görevler bitince açılacak Final UI objesi")]
    [SerializeField] private GameObject finalUIObject;
    [Tooltip("Final konuşması bitince yüklenecek sahne index'i (Ana Menü)")]
    [SerializeField] private int mainMenuSceneIndex = 0;

    // Final narrator skip flag'i
    private bool finalNarratorSkipped = false;
    private bool isFinalNarratorActive = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Tüm görev objeleri başlangıçta kapalı — sıraları geldikçe açılacak
        if (mission1Object        != null) mission1Object.SetActive(false);
        if (mission2Object        != null) mission2Object.SetActive(false);
        if (mission3Object        != null) mission3Object.SetActive(false);
        if (mission4Object        != null) mission4Object.SetActive(false);
        if (mission5TriggerObject != null) mission5TriggerObject.SetActive(false);
        if (mission5PathObject    != null) mission5PathObject.SetActive(false);

        StartCoroutine(StartGameRoutine());
    }

    private void Update()
    {
        // ── Başlangıç Narrator Skip ──────────────────────────────────────
        if (isNarratorActive && !narratorSkipped)
        {
            if (IsSkipButtonPressed()) SkipNarrator();
        }

        // ── Tutorial Input Kontrolü ──────────────────────────────────────
        if (tutorialActive && !turnDone)
            UpdateTutorial();

        // ── Final Narrator Skip ──────────────────────────────────────────
        if (isFinalNarratorActive && !finalNarratorSkipped)
        {
            if (IsSkipButtonPressed()) SkipFinalNarrator();
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Tutorial Mantığı
    // ─────────────────────────────────────────────────────────────────────
    private void UpdateTutorial()
    {
        Vector2 moveAxis = GetLeftStickAxis();
        Vector2 turnAxis = GetRightStickAxis();

        // 1) Oyuncu hareket etti → hareket tutorialini kapat, dönme tutorialini aç
        if (!moveDone && moveAxis.magnitude > tutorialMoveThreshold)
        {
            moveDone = true;
            if (moveTutorialObj != null)
            {
                Destroy(moveTutorialObj);
                Debug.Log("[Tutorial] Hareket tutoriali tamamlandı.");
            }
            if (rotateTutorialObj != null)
            {
                rotateTutorialObj.SetActive(true);
                Debug.Log("[Tutorial] Dönme tutoriali açıldı.");
            }
        }

        // 2) Oyuncu döndü → her iki objeyi de yok et, afterTutorialObject'i aktif et
        if (moveDone && !turnDone && turnAxis.magnitude > tutorialTurnThreshold)
        {
            turnDone = true;
            tutorialActive = false; // Artık tutorial takibi durdu
            if (moveTutorialObj   != null) Destroy(moveTutorialObj);
            if (rotateTutorialObj  != null) Destroy(rotateTutorialObj);
            if (afterTutorialObject != null) afterTutorialObject.SetActive(true);
            Debug.Log("[Tutorial] Tüm tutoriallar tamamlandı. AfterTutorialObject aktif edildi.");
        }
    }

    private Vector2 GetLeftStickAxis()
    {
        // Klavye fallback (Editor testi için)
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f) return new Vector2(h, v);

        var devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, devices);
        foreach (var d in devices)
            if (d.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 axis))
                return axis;
        return Vector2.zero;
    }

    private Vector2 GetRightStickAxis()
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, devices);
        foreach (var d in devices)
            if (d.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 axis))
                return axis;
        return Vector2.zero;
    }


    // A/X tuşu veya klavye Space/Return ile skip kontrolü
    private bool IsSkipButtonPressed()
    {
        // Klavye fallback
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            return true;

        // Sağ A tuşu
        var rightDevices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, rightDevices);
        foreach (var device in rightDevices)
            if (device.TryGetFeatureValue(CommonUsages.primaryButton, out bool p) && p)
                return true;

        // Sol X tuşu
        var leftDevices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, leftDevices);
        foreach (var device in leftDevices)
            if (device.TryGetFeatureValue(CommonUsages.primaryButton, out bool p) && p)
                return true;

        return false;
    }

    private void SkipNarrator()
    {
        narratorSkipped = true;
        Debug.Log("[Narrator] Atlandı.");
    }

    private void SkipFinalNarrator()
    {
        finalNarratorSkipped = true;
        Debug.Log("[Final Narrator] Atlandı.");
    }

    private IEnumerator StartGameRoutine()
    {
        isNarratorActive = true;
        narratorSkipped  = false;

        if (narratorUIObject != null) narratorUIObject.SetActive(true);
        

        float waitTime = overrideWaitTime;
        if (narratorAudioSource != null && narratorAudioSource.clip != null)
        {
            waitTime = narratorAudioSource.clip.length;
            narratorAudioSource.Play();
        }
        else if (waitTime <= 0f) waitTime = 3f;

        Debug.Log("Narrator konuşuyor... " + waitTime + "s bekleniyor.");

        float elapsed = 0f;
        while (elapsed < waitTime && !narratorSkipped)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (narratorSkipped && narratorAudioSource != null)
            narratorAudioSource.Stop();

        isNarratorActive = false;

        if (narratorUIObject != null) narratorUIObject.SetActive(false);
        if (moveObject       != null) moveObject.SetActive(true);
        if (mission1Object   != null) mission1Object.SetActive(true);

        // Hareket tutorialini aç ve izlemeyi başlat
        if (moveTutorialObj != null)
        {
            moveTutorialObj.SetActive(true);
            Debug.Log("[Tutorial] Hareket tutoriali açıldı.");
        }
        if (rotateTutorialObj != null) rotateTutorialObj.SetActive(false); // Başlangıçta kapalı
        tutorialActive = true;

        UpdateMissionText(mission1Text);
        Debug.Log("Narrator bitti. Oyun başlıyor!");
    }

    private void UpdateMissionText(string newText)
    {
        if (missionText != null) missionText.text = newText;
    }

    private void PlayMissionAudio(AudioClip sfxClip, AudioClip narratorClip)
    {
        if (sfxAudioSource != null && sfxClip != null)
            sfxAudioSource.PlayOneShot(sfxClip);

        if (narratorAudioSource != null && narratorClip != null)
        {
            narratorAudioSource.clip = narratorClip;
            narratorAudioSource.Play();
        }
    }

    public void CompleteMission(int missionIndex)
    {
        // Dinleyicilere (AudioTriggerZone vb.) görevin tamamlandığını bildir
        OnMissionCompleted?.Invoke(missionIndex);

        switch (missionIndex)
        {
            case 1:
                if (mission1Object != null) { Destroy(mission1Object); mission1Object = null; }
                UpdateMissionText(mission2Text);
                PlayMissionAudio(missionSuccessSound, mission1NarratorSpeech);
                if (mission2Object != null) mission2Object.SetActive(true);
                break;

            case 2:
                if (mission2Object != null) mission2Object.SetActive(false);
                UpdateMissionText(mission3Text);
                PlayMissionAudio(missionSuccessSound, mission2NarratorSpeech);
                if (mission3Object != null) mission3Object.SetActive(true);
                break;

            case 3:
                if (mission3Object != null) mission3Object.SetActive(false);
                UpdateMissionText(mission4Text);
                PlayMissionAudio(missionSuccessSound, mission3NarratorSpeech);
                if (mission4Object != null) mission4Object.SetActive(true);
                break;

            case 4:
                if (mission4Object != null) mission4Object.SetActive(false);
                UpdateMissionText(mission5Text);
                PlayMissionAudio(missionSuccessSound, mission4NarratorSpeech);
                if (mission5TriggerObject != null) mission5TriggerObject.SetActive(true);
                if (mission5PathObject    != null) mission5PathObject.SetActive(true);
                break;

            case 5:
                if (mission5TriggerObject != null) mission5TriggerObject.SetActive(false);
                if (mission5PathObject    != null) mission5PathObject.SetActive(false);
                UpdateMissionText(missionEndText);
                if (sfxAudioSource != null && missionSuccessSound != null)
                    sfxAudioSource.PlayOneShot(missionSuccessSound);
                StartCoroutine(Mission5EndRoutine());
                Debug.Log("Son Görev tamamlandı!");
                break;

            default:
                Debug.LogWarning("Geçersiz görev numarası: " + missionIndex);
                break;
        }
    }

    private IEnumerator Mission5EndRoutine()
    {
        // Görev 5 ile birlikte gelen objeleri aç/kapat
        if (mission5ObjectsToActivate != null)
            foreach (var obj in mission5ObjectsToActivate)
                if (obj != null) obj.SetActive(true);

        if (mission5ObjectsToDeactivate != null)
            foreach (var obj in mission5ObjectsToDeactivate)
                if (obj != null) obj.SetActive(false);

        // Final UI'ı aç
        if (finalUIObject != null)
            finalUIObject.SetActive(true);

        // Final Narrator konuşmasını başlat
        isFinalNarratorActive  = true;
        finalNarratorSkipped   = false;

        float waitTime = 3f;
        if (narratorAudioSource != null && mission5NarratorSpeech != null)
        {
            narratorAudioSource.clip = mission5NarratorSpeech;
            narratorAudioSource.Play();
            waitTime = mission5NarratorSpeech.length;
        }

        Debug.Log("[Final] Bitiş konuşması bekleniyor: " + waitTime + "s");

        float elapsed = 0f;
        while (elapsed < waitTime && !finalNarratorSkipped)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Skip yapıldıysa sesi durdur
        if (finalNarratorSkipped && narratorAudioSource != null)
            narratorAudioSource.Stop();

        isFinalNarratorActive = false;

        Debug.Log("[Final] Ana Menüye dönülüyor (index: " + mainMenuSceneIndex + ")");
        SceneManager.LoadScene(mainMenuSceneIndex);
    }
}
