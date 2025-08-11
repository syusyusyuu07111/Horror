using UnityEngine;
using UnityEngine.UI;

public class UIImageSimpleFade : MonoBehaviour
{
    [Header("下: 光ってない画像 / 上: 光ってる画像")]
    public Image normalImage;   // 表示は常時 そのまま
    public Image glowImage;     // これのアルファを上げる

    [Header("設定")]
    public float fadeDuration = 1.0f; // 0→1 にかける時間
    public bool playOnStart = true;   // 自動再生

    float t = 0f;
    bool playing = false;

    void Start()
    {
        if (!glowImage) { Debug.LogError("[UIImageSimpleFade] glowImage 未設定"); return; }

        // 上の画像を完全透明に初期化
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
