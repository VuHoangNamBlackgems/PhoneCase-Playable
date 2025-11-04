using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class StateIcon : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Image subIcon;
    [SerializeField] private Sprite onIconSprite;
    [SerializeField] private Sprite offIconSprite;

    [SerializeField] private Sprite onSubIconSprite;
    [SerializeField] private Sprite offSubIconSprite;

    private bool isOn = true;

    public void Set(bool value)
    {
        isOn = value;

        if (!icon) icon = GetComponent<Image>();
        if (icon)
        {
            icon.sprite = isOn ? onIconSprite : offIconSprite;
            subIcon.sprite = isOn ? onSubIconSprite : offSubIconSprite;
        }
    }
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!icon) icon = GetComponent<Image>();
        if (!subIcon) subIcon = transform.GetChild(0).GetComponent<Image>();
        Set(isOn);
    }
#endif
}