using System.Collections;
using UnityEngine;
using TMPro;

public class NovelLooperTMP : MonoBehaviour
{
    [Header("�\����iTextMeshPro - Text (UI)�j")]
    public TMP_Text uiText;

    [Header("�y�[�W�i1�v�f = 1���b�Z�[�W�j")]
    [TextArea(2, 6)]
    public string[] pages;

    [Header("�^�C�v���C�^�[")]
    [Tooltip("1����������̕\���Ԋu�i�b�j")]
    public float charDelay = 0.03f;
    [Tooltip("���b�`�e�L�X�g�i<b> ���j���g��")]
    public bool useRichText = true;

    [Header("�t�F�[�h���o�i�C�Ӂj")]
    [Tooltip("�t�F�[�h�Ɏg�� CanvasGroup�i���w��Ȃ玩���ǉ��j")]
    public CanvasGroup canvasGroup;
    public float fadeInTime = 0.15f;
    public float fadeOutTime = 0.15f;

    [Header("����")]
    [Tooltip("���N���b�N/�X�y�[�X/Enter�Ői��")]
    public bool useMouseOrKeys = true;

    [Header("�Ō�̋���")]
    [Tooltip("�S�y�[�W��ɍŏ��֖߂��ČJ��Ԃ�")]
    public bool loop = true;

    int index = 0;
    bool isTyping = false;
    bool requestSkip = false;
    bool advancePressed = false;

    void Awake()
    {
        if (!uiText)
        {
            Debug.LogError($"{nameof(NovelLooperTMP)}: uiText ���ݒ�ł��BTMP�̃e�L�X�g�����蓖�ĂĂ��������B");
            enabled = false; return;
        }
        uiText.richText = useRichText;

        if (!canvasGroup)
        {
            // �t�F�[�h�p�Ɏ����ŕt�^�iUI Text �̐e�ł��j
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
            // 1) �\���i�^�C�v���C�^�[�j
            uiText.text = "";
            yield return FadeTo(1f, fadeInTime);

            yield return StartCoroutine(TypePage(pages[index]));

            // 2) ���֐i�ޓ��͑҂��i�N���b�N/�L�[�j
            yield return WaitAdvance();

            // 3) ������i�t�F�[�h�A�E�g�j
            yield return FadeTo(0f, fadeOutTime);

            // 4) ���y�[�W��
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
            // ���͂�������X�L�b�v�v���ɐ؂�ւ���
            if (advancePressed) { advancePressed = false; requestSkip = true; }
            else yield return new WaitForSeconds(charDelay);
        }

        isTyping = false;
        yield return null;
    }

    IEnumerator WaitAdvance()
    {
        // �^�C�v���ɃN���b�N���ꂽ�ꍇ�͏�őS���\���ς݂Ȃ̂ŁA
        // �����ł́u���̃N���b�N�v��҂�
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

    // �{�^������Ăׂ�i�s�g���K�i�C�Ӂj
    public void OnAdvanceButton() => advancePressed = true;

    // �O������y�[�W�������ւ��������i�C�Ӂj
    public void SetPages(string[] newPages, bool restart = true)
    {
        pages = newPages;
        if (restart) { StopAllCoroutines(); index = 0; StartCoroutine(MainLoop()); }
    }
}
