using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class EpilogueText : MonoBehaviour
{
    [Header("UI�e�L�X�g")]
    public TMP_Text uiText;

    [Header("�\�����镶�́i�s���Ɓj")]
    [TextArea(2, 10)]
    public string[] lines;

    [Header("�\�����x�i1����������b�j")]
    public float charDelay = 0.05f;

    [Header("�s�ƍs�̊Ԃ̑ҋ@����")]
    public float lineDelay = 1f;

    [Header("�Ō�ɑJ�ڂ���V�[����")]
    public string nextSceneName = "Ingame";

    private bool skipLine = false;

    private void Start()
    {
        uiText.text = "";
        StartCoroutine(ShowEpilogue());
    }

    private void Update()
    {
        // �}�E�X�N���b�N or �X�y�[�X�L�[�ŃX�L�b�v�t���O�𗧂Ă�
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            skipLine = true;
        }
    }

    private IEnumerator ShowEpilogue()
    {
        foreach (string line in lines)
        {
            skipLine = false;
            uiText.text += ""; // �ی�

            // 1�s���ꕶ�����ǉ�
            for (int i = 0; i < line.Length; i++)
            {
                if (skipLine)
                {
                    // �X�L�b�v �� �c��̑S������C�ɕ\��
                    uiText.text += line.Substring(i);
                    break;
                }
                uiText.text += line[i];
                yield return new WaitForSeconds(charDelay);
            }

            // �s�I��� �� ���s
            uiText.text += "\n";

            // ���̍s�܂ł̑ҋ@�i�X�L�b�v���ꂽ�瑦�i�ށj
            float timer = 0f;
            while (timer < lineDelay && !skipLine)
            {
                timer += Time.deltaTime;
                yield return null;
            }
        }

        // �S���\��������V�[���J��
        SceneManager.LoadScene(nextSceneName);
    }
}
