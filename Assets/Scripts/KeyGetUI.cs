using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KeyGetUI : MonoBehaviour
{
    [Header("�ꎞ�\���i�g�[�X�g�j")]
    public CanvasGroup toastGroup;     // �擾�����u�Ԃɒ������ֈꎞ�\��
    public Image toastIcon;
    public TMP_Text toastText;         // ��������Ȃ���Ζ��ݒ��OK
    public float fadeIn = 0.15f;
    public float showSecs = 1.2f;
    public float fadeOut = 0.25f;

    [Header("�������A�C�R���i�펞�\���j")]
    public Image ownedIcon;            // �E��Ȃǂɏ펞�\�����鏬����Image
    public bool showOwnedIcon = true;

    Coroutine _running;

    /// �����擾�����Ƃ��ɌĂ�
    public void ShowKeyObtained(Sprite icon, string label = "�������I")
    {
        if (ownedIcon && showOwnedIcon)
        {
            ownedIcon.sprite = icon;
            ownedIcon.gameObject.SetActive(true);
        }

        if (!toastGroup || !toastIcon) return;

        toastIcon.sprite = icon;
        if (toastText) toastText.text = label;

        if (_running != null) StopCoroutine(_running);
        _running = StartCoroutine(ToastRoutine());
    }

    IEnumerator ToastRoutine()
    {
        toastGroup.gameObject.SetActive(true);
        toastGroup.interactable = false;
        toastGroup.blocksRaycasts = false;

        float t = 0f;
        while (t < fadeIn) { t += Time.unscaledDeltaTime; toastGroup.alpha = t / fadeIn; yield return null; }

        toastGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(showSecs);

        t = 0f;
        while (t < fadeOut) { t += Time.unscaledDeltaTime; toastGroup.alpha = 1f - t / fadeOut; yield return null; }

        toastGroup.alpha = 0f;
        toastGroup.gameObject.SetActive(false);
        _running = null;
    }
}
