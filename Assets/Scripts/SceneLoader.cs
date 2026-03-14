using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using System.Collections.Generic;

public class SceneLoader : MonoBehaviour
{
    [Tooltip("Geçilecek sahnenin adı")]
    [SerializeField] private string targetSceneName;

    // Çift tetiklemeyi önlemek için static flag (sahneler arası korunur)
    private static bool s_buttonWasPressed = false;

    private void Update()
    {
        // XR Secondary Button (Y) veya Klavye Y tuşu
        bool isPressed = IsSecondaryButtonPressed() || Input.GetKey(KeyCode.Y);

        // Sadece tuşa basıldığı anda (rising edge) tetikle
        if (isPressed && !s_buttonWasPressed)
        {
            LoadScene();
        }

        s_buttonWasPressed = isPressed;
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
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogWarning("SceneLoader: targetSceneName is empty!");
        }
    }
}
