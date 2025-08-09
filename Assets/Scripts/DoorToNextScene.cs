using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorToNextScene : MonoBehaviour
{
    [Header("次に読み込むシーン名")]
    public string nextSceneName = "NextScene";

    private void OnTriggerEnter(Collider other)
    {
        // プレイヤーに触れたら
        if (other.CompareTag("Player"))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
