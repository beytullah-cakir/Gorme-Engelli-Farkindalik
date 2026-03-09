using UnityEngine;

public class MissionTrigger : MonoBehaviour
{
    [Header("Görev Ayarları")]
    [Tooltip("Bu görev kaçıncı görev? (Örneğin ilk görev için 1, ikinci için 2...)")]
    public int missionIndex = 1;

    // Eğer objelerinizin Collider'ında "Is Trigger" SEÇİLİ DEĞİLSE bu metod çalışır:
    private void OnCollisionEnter(Collision collision)
    {
        CheckCollision(collision.gameObject.tag);
    }

    // Eğer objelerinizin Collider'ında "Is Trigger" SEÇİLİ İSE bu metod çalışır:
    private void OnTriggerEnter(Collider other)
    {
        CheckCollision(other.tag);
    }

    // Çarpan objenin etiketinin "Player" olup olmadığını kontrol ediyoruz
    private void CheckCollision(string otherTag)
    {
         Debug.Log("Triggered ! tag: " + otherTag);
        if (otherTag == "Player")
        {
            Debug.Log("Trigg");
            if (GameManager.Instance != null)
            {
                // GameManager'a bu görevin tamamlandığı bilgisini yolluyoruz.
                GameManager.Instance.CompleteMission(missionIndex);
            }
            else
            {
                Debug.LogWarning("Sahnede GameManager bulunamadı veya içindeki Instance tanımlanmadı!");
            }
        }
    }
}
