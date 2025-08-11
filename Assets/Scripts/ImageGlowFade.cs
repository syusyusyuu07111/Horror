using UnityEngine;
using UnityEngine.UI;

public class UIImageSimpleFade : MonoBehaviour
{
    [Header("��: �����ĂȂ��摜 / ��: �����Ă�摜")]
    public Image normalImage;   // �\���͏펞 ���̂܂�
    public Image glowImage;     // ����̃A���t�@���グ��

    [Header("�ݒ�")]
    public float fadeDuration = 1.0f; // 0��1 �ɂ����鎞��
    public bool playOnStart = true;   // �����Đ�

    float t = 0f;
    bool playing = false;

    void Start()
    {
        if (!glowImage) { Debug.LogError("[UIImageSimpleFade] glowImage ���ݒ�"); return; }

        // ��̉摜�����S�����ɏ�����
        var c = glowImage.color; c.a = 0f; glowImage.color = c;

        if (playOnStart) Play();
    }

    public void Play()
    {
        if (!glowImage) return;
        t = 0f;
        playing = true;
    }

    void Update()
    {
        if (!playing || !glowImage) return;
        t += Time.deltaTime / Mathf.Max(0.0001f, fadeDuration);
        float a = Mathf.Clamp01(t);

        var c = glowImage.color;
        c.a = a;
        glowImage.color = c;

        if (a >= 1f) playing = false;
    }
}
