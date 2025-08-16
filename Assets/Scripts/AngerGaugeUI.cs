using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class AngerGaugeUI : MonoBehaviour
{
    [Header("画像1枚モード（UI/BottomTintFill を使用）")]
    public bool singleImageMode = true;
    [Tooltip("1枚モードで色を乗せる対象")]
    public Image baseImage;

    [Range(0f, 0.25f)] public float edgeSoftness = 0.06f; // 境界のにじみ
    [Tooltip("0=Multiply(推奨), 1=Add")]
    [Range(0f, 1f)] public float blendMode = 0f;

    [Header("オーバーレイ方式（画像2枚：従来通り）")]
    [Tooltip("同じSpriteを重ね、Filled Verticalで下から色を付ける")]
    public Image overlayImage;

    [Header("色グラデーション（0=穏やか → 1=激怒）")]
    public Gradient colorByLevel;

    // Single-image 用マテリアル
    Material _matInst;
    static readonly int ID_Fill = Shader.PropertyToID("_Fill");
    static readonly int ID_OverlayCol = Shader.PropertyToID("_OverlayColor");
    static readonly int ID_Softness = Shader.PropertyToID("_Softness");
    static readonly int ID_BlendMode = Shader.PropertyToID("_BlendMode");

    void Reset()
    {
        if (!baseImage) baseImage = GetComponentInChildren<Image>();
        SetupOverlayDefaults();
        EnsureMaterialIfSingle();
        SetLevel(0f);
    }

    void OnEnable()
    {
        EnsureMaterialIfSingle();
        SetupOverlayDefaults();
    }

    void OnDisable()
    {
        if (_matInst)
        {
            if (Application.isPlaying) Destroy(_matInst);
            else DestroyImmediate(_matInst);
            _matInst = null;
        }
    }

    void SetupOverlayDefaults()
    {
        if (overlayImage)
        {
            overlayImage.type = Image.Type.Filled;
            overlayImage.fillMethod = Image.FillMethod.Vertical;
            overlayImage.fillOrigin = 0; // 下から
            overlayImage.fillAmount = 0f;
            if (colorByLevel != null) overlayImage.color = colorByLevel.Evaluate(0f);
        }
    }

    void EnsureMaterialIfSingle()
    {
        if (!singleImageMode || !baseImage) return;

        // 既存で正しいシェーダーなら複製して使う（共有マテリアル破壊回避）
        if (_matInst && _matInst.shader && _matInst.shader.name == "UI/BottomTintFill")
        {
            baseImage.material = _matInst;
            return;
        }

        if (baseImage.material && baseImage.material.shader &&
            baseImage.material.shader.name == "UI/BottomTintFill")
        {
            _matInst = new Material(baseImage.material);
        }
        else
        {
            var sh = Shader.Find("UI/BottomTintFill");
            if (!sh)
            {
                Debug.LogWarning("[AngerGaugeUI] Shader 'UI/BottomTintFill' が見つからないため、1枚モードを使えません。overlayImage方式にフォールバックします。");
                singleImageMode = false;
                return;
            }
            _matInst = new Material(sh);
        }

        _matInst.hideFlags = HideFlags.DontSave;
        baseImage.material = _matInst;

        // 初期値
        _matInst.SetFloat(ID_Fill, 0f);
        _matInst.SetFloat(ID_Softness, edgeSoftness);
        _matInst.SetFloat(ID_BlendMode, blendMode);
        var c0 = (colorByLevel != null) ? colorByLevel.Evaluate(0f) : Color.red;
        _matInst.SetColor(ID_OverlayCol, c0);
    }

    /// 0..1 でゲージを更新
    public void SetLevel(float t01)
    {
        float t = Mathf.Clamp01(t01);

        if (singleImageMode && _matInst)
        {
            _matInst.SetFloat(ID_Fill, t);
            _matInst.SetFloat(ID_Softness, edgeSoftness);
            _matInst.SetFloat(ID_BlendMode, blendMode);
            if (colorByLevel != null)
                _matInst.SetColor(ID_OverlayCol, colorByLevel.Evaluate(t));
        }
        else
        {
            // 2枚方式（従来）
            if (!overlayImage) return;
            overlayImage.fillAmount = t;
            if (colorByLevel != null) overlayImage.color = colorByLevel.Evaluate(t);
        }
    }
}
