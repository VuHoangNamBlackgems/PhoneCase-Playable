using TMPro;
using UnityEngine;
using DG.Tweening;

public class UICoinCounter : MonoBehaviour
{
    [SerializeField] TMP_Text coinText;
    [SerializeField] RectTransform targetAnchor;    
    [SerializeField] float countDuration = 0.5f;
    [SerializeField] Ease countEase = Ease.OutCubic;

    int current;
    Tweener countTween;
    public RectTransform Target => targetAnchor;

    void Awake()
    {
        if (!coinText) coinText = GetComponentInChildren<TMP_Text>(true);
        if (!targetAnchor) targetAnchor = transform as RectTransform;
    }

    void OnEnable()
    {
        current = UserInventory.GetCurrencyValue(CurrencyType.CASH);
        coinText.text = current.ToString();
        UserInventory.CurrencyChangedHandler += OnCashChanged;
    }
    void OnDisable()
    {
        UserInventory.CurrencyChangedHandler -= OnCashChanged;
        countTween?.Kill();
    }

    void OnCashChanged(CurrencyType type, int _, int __)
    {
        if (type != CurrencyType.CASH) return;

        int next = UserInventory.GetCurrencyValue(CurrencyType.CASH);
        countTween?.Kill();

        countTween = DOTween.To(() => current, v =>
        {
            coinText.text = v.ToString();
        }, next, countDuration).SetEase(countEase);

        (transform as RectTransform).DOPunchScale(Vector3.one * 0.12f, 0.2f, 8, 0.9f);
        current = next;
    }

}