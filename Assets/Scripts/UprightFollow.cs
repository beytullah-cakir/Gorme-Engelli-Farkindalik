using UnityEngine;

public class UprightFollow : MonoBehaviour
{
    [Header("Settings")]
    public Transform headCamera; // Drag your Main Camera here
    public float distance = 2.0f; // How far away the menu floats
    public float heightOffset = 0f; // Adjust this if it's too high/low

    void Update()
    {
        if (headCamera == null) return;

        // 1. Calculate the "Flat" Forward direction (ignoring looking up/down)
        Vector3 flatForward = headCamera.forward;
        flatForward.y = 0; // Flatten it
        flatForward.Normalize(); // Keep the length correct

        // 2. POSITION: Place the UI in front of the player, using that flat direction
        // We use headCamera.position.y so the menu follows your height (sitting/standing)
        Vector3 targetPosition = headCamera.position + (flatForward * distance);
        targetPosition.y += heightOffset;
        
        transform.position = targetPosition;

        // 3. ROTATION: Make the UI face the same way the player is facing (but upright)
        if (flatForward != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(flatForward);
        }
    }
}