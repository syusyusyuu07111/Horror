using System.Collections;
using UnityEngine;
using TMPro;

public class NovelLooperTMP : MonoBehaviour
{
    [Header("表示先（TextMeshPro - Text (UI)）")]
    public TMP_Text uiText;

    [Header("ページ（1要素 = 1メッセージ）")]
    [TextArea(2, 6)]
    public string[] pages;

    [Header("タイプライター")]
    [Tooltip("1文字あたりの表示間隔（秒）")]
    public float charDelay = 0.03f;
    [Tooltip("リッチテキスト（<b> 等）を使う")]
    public bool useRichText = true;

    [Header("フェード演出（任意）")]
    [Tooltip("フェードに使う CanvasGroup（未指定なら自動追加）")]
    public CanvasGroup canvasGroup;
    public float fadeInTime = 0.15f;
    public float fadeOutTime = 0.15f;

    [Header("入力")]
    [Tooltip("左クリック/スペース/Enterで進む")]
    public bool useMouseOrKeys = true;

    [Header("最後の挙動")]
    [Tooltip("全ページ後に最初へ戻って繰り返す")]
    public bool loop = true;

    int index = 0;
    bool isTyping = false;
    bool requestSkip = false;
    bool advancePressed = false;

    void Awake()
    {
        if (!uiText)
        {
            Debug.LogError($"{nameof(NovelLooperTMP)}: uiText 未設定です。TMPのテキストを割り当ててください。");
            enabled = false; return;
        }
        uiText.richText = useRichText;

        if (!canvasGroup)
        {
            // フェード用に自動で付与（UI Text の親でも可）
            canvasGroup = uiText.GetComponent<CanvasGroup>();
            if (!canvasGroup) canvasGroup = uiText.gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 0f;
    }

    void Start()
    {
        StartCoroutine(MainLoop());
    }

    void Update()
    {
        if (!useMouseOrKeys) return;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            advancePressed = true;
    }

    IEnumerator MainLoop()
    {
        if (pages == null || pages.Length == 0) yield break;

        while (true)
        {
            // 1) 表示（タイプライター）
            uiText.text = "";
            yield return FadeTo(1f, fadeInTime);

            yield return StartCoroutine(TypePage(pages[index]));

            // 2) 次へ進む入力待ち（クリック/キー）
            yield return WaitAdvance();

            // 3) 消える（フェードアウト）
            yield return FadeTo(0f, fadeOutTime);

            // 4) 次ページへ
            index++;
            if (index >= pages.Length)
            {
                if (loop) index = 0;
                else break;
            }
        }
    }

    IEnumerator TypePage(string text)
    {
        isTyping = true;
        requestSkip = false;

        for (int i = 0; i < text.Length; i++)
        {
            if (requestSkip) { uiText.text = text; break; }
            uiText.text += text[i];
            // 入力が来たらスキップ要求に切り替える
            if (advancePressed) { advancePressed = false; requestSkip = true; }
            else yield return new WaitForSeconds(charDelay);
        }

        isTyping = false;
        yield return null;
    }

    IEnumerator WaitAdvance()
    {
        // タイプ中にクリックされた場合は上で全文表示済みなので、
        // ここでは「次のクリック」を待つ
        advancePressed = false;
        while (!advancePressed)
            yield return null;
        advancePressed = false;
    }

    IEnumerator FadeTo(float target, float duration)
    {
        float start = canvasGroup ? canvasGroup.alpha : 1f;
        if (duration <= 0f)
        {
            if (canvasGroup) canvasGroup.alpha = target;
            yield break;
        }
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(start, target, t / duration);
            if (canvasGroup) canvasGroup.alpha = a;
            yield return null;
        }
        if (canvasGroup) canvasGroup.alpha = target;
    }

    // ボタンから呼べる進行トリガ（任意）
    public void OnAdvanceButton() => advancePressed = true;

    // 外部からページを差し替えたい時（任意）
    public void SetPages(string[] newPages, bool restart = true)
    {
        pages = newPages;
        if (restart) { StopAllCoroutines(); index = 0; StartCoroutine(MainLoop()); }
    }
}
