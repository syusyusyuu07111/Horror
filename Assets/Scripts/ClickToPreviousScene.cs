using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// �N���b�N���ꂽ��u�Ō�ɕۑ����Ă������V�[���v�֖߂�܂��B
/// �ۑ��������ꍇ�� defaultSceneName �ɑJ�ڂ��܂��B
/// </summary>
[RequireComponent(typeof(Image))]
public class ClickToPreviousScene : MonoBehaviour, IPointerClickHandler
{
    [Header("�ۑ��������ꍇ�̑J�ڐ�i�C�Ӂj")]
    public string defaultSceneName = "Ingame";

    public void OnPointerClick(PointerEventData eventData)
    {
        // �ۑ�����Ă��钼�O�V�[���ցi������΃f�t�H���g�ցj
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
