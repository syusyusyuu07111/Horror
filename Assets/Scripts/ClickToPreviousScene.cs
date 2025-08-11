using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// クリックされたら「最後に保存しておいたシーン」へ戻ります。
/// 保存が無い場合は defaultSceneName に遷移します。
/// </summary>
[RequireComponent(typeof(Image))]
public class ClickToPreviousScene : MonoBehaviour, IPointerClickHandler
{
    [Header("保存が無い場合の遷移先（任意）")]
    public string defaultSceneName = "Ingame";

    public void OnPointerClick(PointerEventData eventData)
    {
        // 保存されている直前シーンへ（無ければデフォルトへ）
        string last = SceneTracker.GetLastScene();
        if (string.IsNullOrEmpty(last)) last = defaultSceneName;

        if (string.IsNullOrEmpty(last))
        {
            Debug.LogWarning("[ClickToPreviousScene] No last scene saved and defaultSceneName is empty.");
            return;
        }

        SceneManager.LoadScene(last);
    }
}
