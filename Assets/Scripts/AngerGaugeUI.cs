using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class AngerGaugeUI : MonoBehaviour
{
    [Header("�摜1�����[�h�iUI/BottomTintFill ���g�p�j")]
    public bool singleImageMode = true;
    [Tooltip("1�����[�h�ŐF���悹��Ώ�")]
    public Image baseImage;

    [Range(0f, 0.25f)] public float edgeSoftness = 0.06f; // ���E�̂ɂ���
    [Tooltip("0=Multiply(����), 1=Add")]
    [Range(0f, 1f)] public float blendMode = 0f;

    [Header("�I�[�o�[���C�����i�摜2���F�]���ʂ�j")]
    [Tooltip("����Sprite���d�ˁAFilled Vertical�ŉ�����F��t����")]
    public Image overlayImage;

    [Header("�F�O���f�[�V�����i0=���₩ �� 1=���{�j")]
    public Gradient colorByLevel;

    // Single-image �p�}�e���A��
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
            overlayImage.fillOrigin = 0; // ������
            overlayImage.fillAmount = 0f;
            if (colorByLevel != null) overlayImage.color = colorByLevel.Evaluate(0f);
        }
    }

    void EnsureMaterialIfSingle()
    {
        if (!singleImageMode || !baseImage) return;

        // �����Ő������V�F�[�_�[�Ȃ畡�����Ďg���i���L�}�e���A���j�����j
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
                Debug.LogWarning("[AngerGaugeUI] Shader 'UI/BottomTintFill' ��������Ȃ����߁A1�����[�h���g���܂���BoverlayImage�����Ƀt�H�[���o�b�N���܂��B");
                singleImageMode = false;
                return;
            }
            _matInst = new Material(sh);
        }

        _matInst.hideFlags = HideFlags.DontSave;
        baseImage.material = _matInst;

        // �����l
        _matInst.SetFloat(ID_Fill, 0f);
        _matInst.SetFloat(ID_Softness, edgeSoftness);
        _matInst.SetFloat(ID_BlendMode, blendMode);
        var c0 = (colorByLevel != null) ? colorByLevel.Evaluate(0f) : Color.red;
        _matInst.SetColor(ID_OverlayCol, c0);
    }

    /// 0..1 �ŃQ�[�W���X�V
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
            // 2�������i�]���j
            if (!overlayImage) return;
            overlayImage.fillAmount = t;
            if (colorByLevel != null) overlayImage.color = colorByLevel.Evaluate(t);
        }
    }
}
