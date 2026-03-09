using UnityEngine;

public class FollowCameraXZ : MonoBehaviour
{
    [Header("Referanslar")]
    [Tooltip("Takip edilecek Kamera (Boş bırakılırsa sahnede otomatik Main Camera'yı bulur)")]
    public Transform cameraToFollow;

    [Header("Ayarlar")]
    [Tooltip("Belirli bir fark (offset) bırakmak isterseniz X ve Z değerlerini değiştirebilirsiniz.")]
    public Vector3 offset;

    private void Start()
    {
        // Eğer Inspector'dan kamera atanmamışsa otomatik olarak Tag'ı "MainCamera" olan objeyi bulur
        if (cameraToFollow == null)
        {
            if (Camera.main != null)
            {
                cameraToFollow = Camera.main.transform;
            }
            else
            {
                Debug.LogWarning("FollowCameraXZ: Sahnede 'MainCamera' etiketli bir kamera bulunamadı!");
            }
        }
    }

    // LateUpdate kullanmak, kameranın kendi hareketini bitirmesinden SONRA 
    // çalışması anlamına geldiği için titremeleri önler.
    private void LateUpdate()
    {
        if (cameraToFollow != null)
        {
            // Orijinal Y eksenimizi (yüksekliğimizi) sabit tutuyoruz
            float currentY = transform.position.y;

            // X ve Z pozisyonunu kameradan alıp belirlediğimiz Offset'i ekliyoruz
            float targetX = cameraToFollow.position.x + offset.x;
            float targetZ = cameraToFollow.position.z + offset.z;

            // Kendi pozisyonumuzu güncelliyoruz (Y ekseni dokunulmamış şekilde)
            transform.position = new Vector3(targetX, currentY, targetZ);
        }
    }
}
