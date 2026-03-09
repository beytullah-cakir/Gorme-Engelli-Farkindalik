using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using System.Collections.Generic;

public class SceneLoader : MonoBehaviour
{
    [Tooltip("Geçilecek sahnenin Build Settings index'i")]
    [SerializeField] private int targetSceneIndex = 0;

    // Çift tetiklemeyi önlemek için flag
    private bool buttonWasPressed = false;

    private void Update()
    {

        bool isPressed = IsSecondaryButtonPressed();

        // Sadece tuşa basıldığı anda (rising edge) tetikle
        if (isPressed && !buttonWasPressed)
            LoadScene();

        buttonWasPressed = isPressed;
    }

    // Y tuşu (secondaryButton) — tüm bağlı cihazlar taranir
    // Build'de karakteristik filtresi cihazı bulamayabilir,
    // GetAllDevices() daha güvenilirdir.
    private bool IsSecondaryButtonPressed()
    {
        var allDevices = new List<InputDevice>();
        InputDevices.GetDevices(allDevices);

        foreach (var device in allDevices)
            if (device.TryGetFeatureValue(CommonUsages.secondaryButton, out bool pressed) && pressed)
                return true;

        return false;
    }

    public void LoadScene()
    {
        SceneManager.LoadScene(targetSceneIndex);
    }
}
