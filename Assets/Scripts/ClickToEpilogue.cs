using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ClickToEpilogue : MonoBehaviour, IPointerClickHandler
{
    [Header("ˆÚ“®æƒV[ƒ“–¼")]
    public string epilogueSceneName = "Epilogue";

    public void OnPointerClick(PointerEventData eventData)
    {
        SceneManager.LoadScene(epilogueSceneName);
    }
}
