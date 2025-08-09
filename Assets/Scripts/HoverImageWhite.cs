using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class HoverImageWhite : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Image image;
    private Color originalColor;

    [Header("カーソルが乗ったときの色")]
    public Color hoverColor = Color.white;

    void Awake()
    {
        image = GetComponent<Image>();
        originalColor = image.color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        image.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        image.color = originalColor;
    }
}
