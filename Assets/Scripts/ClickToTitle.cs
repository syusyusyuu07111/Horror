using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ClickToTitle : MonoBehaviour, IPointerClickHandler
{
    [Header("�߂�V�[�����i��: TitleScene�j")]
    public string titleSceneName = "Title";

    public void OnPointerClick(PointerEventData eventData)
    {
        SceneManager.LoadScene(titleSceneName);
    }
}
