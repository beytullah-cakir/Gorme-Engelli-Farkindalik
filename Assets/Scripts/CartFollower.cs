using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CartFollower : MonoBehaviour
{
    [Header("Referanslar")]
    [Tooltip("Takip edilecek Kamera (Boş bırakılırsa sahnede otomatik Main Camera'yı bulur)")]
    public Transform cameraToFollow;

    [Header("Mesafe Ayarları")]
    [Tooltip("Kameranın ne kadar önünde (veya arkasında) duracak? Pozitif = Ön, Negatif = Arka")]
    public float forwardOffset = 0.8f;

    [Tooltip("Kameranın ne kadar sağında (veya solunda) duracak? Pozitif = Sağ, Negatif = Sol")]
    public float rightOffset = 0.0f;

    [Header("Rotasyon ve Yükseklik")]
    [Tooltip("Cart'ın kendi Y yüksekliğini (başlangıçtaki boyunu) korusun mu?")]
    public bool keepOriginalY = true;
    
    [Tooltip("Eğer araba yan duruyorsa buradan ekstra dönüş açısı verebilirsiniz (Örn: 90, -90, 180)")]
    public float baseRotationOffsetY = 0f;

    [Header("Ses Ayarları")]
    [Tooltip("Araba hareket ettiğinde çalacak olan AudioSource bileşeni. (Boş bırakılırsa üzerine ekli olanı otomatik alır)")]
    public AudioSource cartAudioSource;
    [Tooltip("Araba ne kadar yer değiştirirse ses çıkmaya başlasın?")]
    public float movementThreshold = 0.01f;
    [Tooltip("Araba ne kadar dönerse ses çıkmaya başlasın?")]
    public float rotationThreshold = 0.5f;

    private float originalY;
    
    // Ses kontrolü için önceki pozisyon ve rotasyonu tutacağız
    private Vector3 lastPosition;
    private float lastYRotation;

    private void Start()
    {
        // 1. Kamerayı bul
        if (cameraToFollow == null)
        {
            if (Camera.main != null)
            {
                cameraToFollow = Camera.main.transform;
            }
            else
            {
                Debug.LogWarning("CartFollower: Sahnede 'MainCamera' etiketli bir kamera bulunamadı!");
            }
        }

        // 2. AudioSource'u bul (Eğer Unity arayüzünde sürüklenmediyse)
        if (cartAudioSource == null)
        {
            cartAudioSource = GetComponent<AudioSource>();
        }

        // 3. Başlangıç yüksekliğini kaydet
        originalY = transform.position.y;
        
        // 4. Başlangıç pozisyon ve dönüşünü kaydet
        lastPosition = transform.position;
        lastYRotation = transform.eulerAngles.y;
    }

    private void LateUpdate()
    {
        if (cameraToFollow != null)
        {
            // --- POZİSYON HESAPLAMA ---
            
            // Kameranın sadece Y eksenindeki dönüşünü (sağa sola bakma) dümdüz alan bir dönüş
            Quaternion cameraFlatRotation = Quaternion.Euler(0, cameraToFollow.eulerAngles.y, 0);

            // Kameranın sadece dümdüz ileri yönünü buluyoruz
            Vector3 forwardDirection = cameraFlatRotation * Vector3.forward * forwardOffset;
            
            // Kameranın sadece dümdüz sağ yönünü buluyoruz
            Vector3 rightDirection = cameraFlatRotation * Vector3.right * rightOffset;

            // Hedef pozisyon: Kamera noktası + İleri mesafe + Sağ mesafe
            Vector3 targetPosition = cameraToFollow.position + forwardDirection + rightDirection;

            // Yüksekliği sabitliyoruz
            if (keepOriginalY)
            {
                targetPosition.y = originalY; 
            }
            else
            {
                targetPosition.y = transform.position.y;
            }

            // Pozisyonu uygula
            transform.position = targetPosition;        

            // --- ROTASYON DEĞERLERİ UYGULAMA ---
            
            // X her zaman -90, Z her zaman -270 olacak.
            // Sadece Y eksenine (sağ sol dönüş) kameranın açısını ekliyoruz!
            float finalYRotation = cameraToFollow.eulerAngles.y + baseRotationOffsetY;

            transform.rotation = Quaternion.Euler(-90f, finalYRotation, -270f);

            // --- SES KONTROLÜ ---
            HandleAudioPlayback(finalYRotation);
        }
    }

    private void HandleAudioPlayback(float currentYRotation)
    {
        if (cartAudioSource == null) return;

        // Pozisyondaki değişimi hesapla (Y ekseni hariç çünkü VR'da kafa sallanınca araba titreyebilir)
        Vector2 currentXZ = new Vector2(transform.position.x, transform.position.z);
        Vector2 lastXZ = new Vector2(lastPosition.x, lastPosition.z);
        float distanceMoved = Vector2.Distance(currentXZ, lastXZ);

        // Dönüşteki değişimi hesapla
        float rotationChange = Mathf.Abs(Mathf.DeltaAngle(currentYRotation, lastYRotation));

        // Eğer belirli bir eşik değerinden fazla hareket ettiyse veya döndüyse sesi çalmaya başla
        if (distanceMoved > movementThreshold || rotationChange > rotationThreshold)
        {
            if (!cartAudioSource.isPlaying)
            {
                cartAudioSource.Play();
            }
        }
        else
        {
            // Hareket durduysa veya çok azaldıysa sesi durdur
            if (cartAudioSource.isPlaying)
            {
                cartAudioSource.Pause(); // Tamamen baştan başlamaması için Pause kullanıyoruz
            }
        }

        // Yeni değerleri bir sonraki frame için kaydet
        lastPosition = transform.position;
        lastYRotation = currentYRotation;
    }
}
