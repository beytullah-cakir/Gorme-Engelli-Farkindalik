using UnityEngine;
using System.Collections.Generic;

public class ItemCollector : MonoBehaviour
{
    [Header("Sepetteki ürün objeleri (sırayla aktif olacak)")]
    public GameObject[] cartItems;

    private int currentItemIndex = 0;

    // Aynı objenin birden fazla Collider'ı sebebiyle iki kez sayılmasını önler
    private HashSet<int> processedItems = new HashSet<int>();

    private void OnTriggerEnter(Collider other)
    {
        // Giren nesnenin Layer'ı "Item" değilse çık
        if (other.gameObject.layer != LayerMask.NameToLayer("Item")) return;

        // Aynı obje daha önce işlendiyse tekrar işleme
        int instanceId = other.gameObject.GetInstanceID();
        if (processedItems.Contains(instanceId)) return;
        processedItems.Add(instanceId);

        // Sahnedeki ürünü yok et
        Destroy(other.gameObject);

        // Sıradaki sepet ürününü aktif et
        if (currentItemIndex < cartItems.Length)
        {
            cartItems[currentItemIndex].SetActive(true);
            currentItemIndex++;
            Debug.Log($"Ürün toplandı! Sepetteki ürün sayısı: {currentItemIndex}");

            // Move objesini GameManager'dan al ve aktif et
            GameObject move = GameManager.Instance != null ? GameManager.Instance.MoveObject : null;
            if (move != null)
            {
                move.SetActive(true);
                Debug.Log($"[ItemCollector] '{move.name}' aktif edildi.");
            }

            // --- GÖREV TAMAMLAMA ---
            // 1. ürün → Mission 2, 2. ürün → Mission 3, 3. ürün → Mission 4
            int missionToComplete = currentItemIndex + 1;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CompleteMission(missionToComplete);
                Debug.Log($"Görev {missionToComplete} tamamlandı!");
            }
            else
            {
                Debug.LogWarning("ItemCollector: Sahnede GameManager bulunamadı!");
            }
        }
        else
        {
            Debug.Log("Sepet dolu!");
        }
    }

    // Sepeti sıfırlamak için (isteğe bağlı)
    public void ResetCart()
    {
        foreach (var item in cartItems)
        {
            item.SetActive(false);
        }
        currentItemIndex = 0;
        processedItems.Clear();
    }
}