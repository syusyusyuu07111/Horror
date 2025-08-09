using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorToNextScene : MonoBehaviour
{
    [Header("���ɓǂݍ��ރV�[����")]
    public string nextSceneName = "NextScene";

    private void OnTriggerEnter(Collider other)
    {
        // �v���C���[�ɐG�ꂽ��
        if (other.CompareTag("Player"))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
