using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 直前にいたシーン名を PlayerPrefs に保存/取得するユーティリティ。
/// 「ゲームオーバーに遷移する前」に SaveCurrentScene() を呼んでください。
/// </summary>
public class SceneTracker : MonoBehaviour
{
    public const string KeyLastScene = "LastScene";

    /// <summary>
    /// 現在のアクティブシーン名を保存します。
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
    /// 保存されている「直前シーン名」を返します（無ければ空文字）。
    /// </summary>
    public static string GetLastScene()
    {
        return PlayerPrefs.GetString(KeyLastScene, string.Empty);
    }

    /// <summary>
    /// 保存があればそれへ、なければ defaultSceneName へロードします。
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
