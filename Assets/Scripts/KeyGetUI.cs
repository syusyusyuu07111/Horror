using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KeyGetUI : MonoBehaviour
{
    [Header("一時表示（トースト）")]
    public CanvasGroup toastGroup;     // 取得した瞬間に中央等へ一時表示
    public Image toastIcon;
    public TMP_Text toastText;         // 文字いらなければ未設定でOK
    public float fadeIn = 0.15f;
    public float showSecs = 1.2f;
    public float fadeOut = 0.25f;

    [Header("所持中アイコン（常時表示）")]
    public Image ownedIcon;            // 右上などに常時表示する小さなImage
    public bool showOwnedIcon = true;

    Coroutine _running;

    /// 鍵を取得したときに呼ぶ
    public void ShowKeyObtained(Sprite icon, string label = "鍵を入手！")
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
