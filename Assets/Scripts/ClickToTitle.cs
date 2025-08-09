using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ClickToTitle : MonoBehaviour, IPointerClickHandler
{
    [Header("戻るシーン名（例: TitleScene）")]
    public string titleSceneName = "Title";

    public void OnPointerClick(PointerEventData eventData)
    {
        SceneManager.LoadScene(titleSceneName);
    }
}
