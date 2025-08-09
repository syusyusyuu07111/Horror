using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class EpilogueText : MonoBehaviour
{
    [Header("UIテキスト")]
    public TMP_Text uiText;

    [Header("表示する文章（行ごと）")]
    [TextArea(2, 10)]
    public string[] lines;

    [Header("表示速度（1文字あたり秒）")]
    public float charDelay = 0.05f;

    [Header("行と行の間の待機時間")]
    public float lineDelay = 1f;

    [Header("最後に遷移するシーン名")]
    public string nextSceneName = "Ingame";

    private bool skipLine = false;

    private void Start()
    {
        uiText.text = "";
        StartCoroutine(ShowEpilogue());
    }

    private void Update()
    {
        // マウスクリック or スペースキーでスキップフラグを立てる
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
            uiText.text += ""; // 保険

            // 1行を一文字ずつ追加
            for (int i = 0; i < line.Length; i++)
            {
                if (skipLine)
                {
                    // スキップ → 残りの全文を一気に表示
                    uiText.text += line.Substring(i);
                    break;
                }
                uiText.text += line[i];
                yield return new WaitForSeconds(charDelay);
            }

            // 行終わり → 改行
            uiText.text += "\n";

            // 次の行までの待機（スキップされたら即進む）
            float timer = 0f;
            while (timer < lineDelay && !skipLine)
            {
                timer += Time.deltaTime;
                yield return null;
            }
        }

        // 全部表示したらシーン遷移
        SceneManager.LoadScene(nextSceneName);
    }
}
