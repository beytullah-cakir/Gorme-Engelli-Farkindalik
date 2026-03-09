using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR;

/// <summary>
/// Stick iki farklı şekilde titreşim verir:
/// 1. "Path" layer → içindeyken SÜREKLİ titreşim
/// 2. "Contact" layer → dokunma ANINDA tek darbe titreşim
/// </summary>
public class StickHaptic : MonoBehaviour
{
    [Header("Path Haptic (Sürekli — İçindeyken)")]
    [Tooltip("Titreşim şiddeti (0.0 - 1.0)")]
    [Range(0f, 1f)]
    [SerializeField] private float pathAmplitude = 0.6f;

    [Tooltip("Her titreşim darbesi süresi (saniye)")]
    [SerializeField] private float pathDuration = 0.05f;

    [Tooltip("Darbeler arasındaki süre (saniye)")]
    [SerializeField] private float pathInterval = 0.04f;

    [Header("Contact Haptic (Tek Darbe — Dokunma Anında)")]
    [Tooltip("Dokunma anında titreşim verecek layer (Inspector'dan seçin)")]
    [SerializeField] private LayerMask contactLayer;

    [Tooltip("Dokunma titreşim şiddeti (0.0 - 1.0)")]
    [Range(0f, 1f)]
    [SerializeField] private float contactAmplitude = 0.8f;

    [Tooltip("Dokunma titreşim süresi (saniye)")]
    [SerializeField] private float contactDuration = 0.12f;

    [Tooltip("Aynı objeye tekrar titreşim için minimum bekleme (saniye)")]
    [SerializeField] private float contactCooldown = 0.2f;



    // --- Path runtime ---
    private float lastPathHapticTime = -999f;
    private int pathOverlapCount = 0;
    private int pathLayer;
    private bool pathEverTouched = false; // İlk temas bayrağı

    /// <summary>
    /// Bastonun şu anda "Path" layer'ıyla temas halinde olup olmadığını döndürür.
    /// SplinePathIndicator bu değeri okur.
    /// </summary>
    public bool IsTouchingPath => pathOverlapCount > 0;


    // --- Contact runtime ---
    private float lastContactHapticTime = -999f;

    private void Awake()
    {
        pathLayer = LayerMask.NameToLayer("Path");
    }

    // ──────────────── PATH: sürekli ────────────────
    private void Update()
    {
        if (pathOverlapCount > 0 && Time.time - lastPathHapticTime >= pathInterval)
        {
            lastPathHapticTime = Time.time;
            SendHaptic(pathAmplitude, pathDuration);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Path: sürekli titreşim başlat
        if (other.gameObject.layer == pathLayer)
        {
            pathOverlapCount++;

            // İlk kez yola dokunulduğunda GameManager'daki objeyi yok et
            if (!pathEverTouched)
            {
                pathEverTouched = true;
                if (GameManager.Instance != null)
                {
                    GameObject toDestroy = GameManager.Instance.PathFirstTouchDestroyObject;
                    if (toDestroy != null)
                    {
                        Destroy(toDestroy);
                        Debug.Log("[StickHaptic] Path'e ilk temas — obje yok edildi.");
                    }
                }
            }

            return;
        }

        // Contact: tek darbe titreşim
        if (((1 << other.gameObject.layer) & contactLayer) != 0)
        {
            if (Time.time - lastContactHapticTime >= contactCooldown)
            {
                lastContactHapticTime = Time.time;
                SendHaptic(contactAmplitude, contactDuration);
                Debug.Log($"[StickHaptic] Contact darbe → {other.gameObject.name}");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == pathLayer)
        {
            pathOverlapCount = Mathf.Max(0, pathOverlapCount - 1);
            if (pathOverlapCount == 0) StopHaptic();
        }
    }



    // ──────────────── Yardımcı ────────────────
    private void SendHaptic(float amplitude, float duration)
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
            devices);

        foreach (var device in devices)
            device.SendHapticImpulse(0, amplitude, duration);
    }

    private void StopHaptic()
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
            devices);

        foreach (var device in devices)
            device.StopHaptics();
    }
}
