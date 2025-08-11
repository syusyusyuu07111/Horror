using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ���O�ɂ����V�[������ PlayerPrefs �ɕۑ�/�擾���郆�[�e�B���e�B�B
/// �u�Q�[���I�[�o�[�ɑJ�ڂ���O�v�� SaveCurrentScene() ���Ă�ł��������B
/// </summary>
public class SceneTracker : MonoBehaviour
{
    public const string KeyLastScene = "LastScene";

    /// <summary>
    /// ���݂̃A�N�e�B�u�V�[������ۑ����܂��B
    /// </summary>
    public static void SaveCurrentScene()
    {
        string current = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetString(KeyLastScene, current);
        PlayerPrefs.Save();
#if UNITY_EDITOR
        Debug.Log($"[SceneTracker] Saved LastScene = {current}");
#endif
    }

    /// <summary>
    /// �ۑ�����Ă���u���O�V�[�����v��Ԃ��܂��i������΋󕶎��j�B
    /// </summary>
    public static string GetLastScene()
    {
        return PlayerPrefs.GetString(KeyLastScene, string.Empty);
    }

    /// <summary>
    /// �ۑ�������΂���ցA�Ȃ���� defaultSceneName �փ��[�h���܂��B
    /// </summary>
    public static void LoadLastOrDefault(string defaultSceneName)
    {
        string target = GetLastScene();
        if (string.IsNullOrEmpty(target)) target = defaultSceneName;
        if (string.IsNullOrEmpty(target))
        {
            Debug.LogWarning("[SceneTracker] No last scene saved and defaultSceneName is empty.");
            return;
        }
        SceneManager.LoadScene(target);
    }
}
