using UnityEngine;
using UnityEngine.SceneManagement;

public class MySceneManager : MonoBehaviour
{
    // Level 1'e gitmek için (Build Index 1 olan sahne)
    public void GoLevel1()
    {
        Debug.Log("Level 1'e (Scene Index 1) geçiş yapılıyor...");
        SceneManager.LoadScene(1);
    }

    // Level 2'ye gitmek için (Build Index 2 olan sahne)
    public void GoLevel2()
    {
        Debug.Log("Level 2'ye (Scene Index 2) geçiş yapılıyor...");
        SceneManager.LoadScene(2);
    }
}
