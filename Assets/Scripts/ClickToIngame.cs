using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ClickToIngame : MonoBehaviour, IPointerClickHandler
{
    [Header("ˆÚ“®æƒV[ƒ“–¼")]
    public string ingameSceneName = "Ingame";

    public void OnPointerClick(PointerEventData eventData)
    {
        SceneManager.LoadScene(ingameSceneName);
    }
}
