using TMPro;
using UnityEngine;

public class CoinTextUI : MonoBehaviour
{
    [SerializeField] TMP_Text coinText;

    void OnEnable()
    {
        coinText.text = UserInventory.GetCurrencyValue(CurrencyType.CASH).ToString();
        UserInventory.CurrencyChangedHandler += OnCurrencyChanged;
    }
    void OnDisable()
    {
        UserInventory.CurrencyChangedHandler -= OnCurrencyChanged;
    }
    void OnCurrencyChanged(CurrencyType type, int __, int ___)
    {
        if (type != CurrencyType.CASH) return;
        coinText.text = UserInventory.GetCurrencyValue(CurrencyType.CASH).ToString();
    }
}