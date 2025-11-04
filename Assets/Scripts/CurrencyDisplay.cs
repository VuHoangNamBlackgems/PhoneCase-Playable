using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class CurrencyDisplay : MonoBehaviour
{
    public float _delayUpdate;
    public float _runUpdate;
    public CurrencyType displayCurrency;
    public string textConvert = "{value}";
    public TMP_Text valueText;

    private Tween _delayTweener;
    private Tweener _runTweener;

    private void Awake()
    {
        UserInventory.CurrencyChangedHandler += Event_OnCashChange;
    }

    private void OnDestroy()
    {
        UserInventory.CurrencyChangedHandler -= Event_OnCashChange;
    }

    private void Start()
    {
        valueText.text = textConvert.Replace("{value}", UserInventory.GetCurrencyValue(displayCurrency).ToString("n0"));
    }

    void Event_OnCashChange(CurrencyType currencyType, int currentValue, int addValue)
    {
        if (displayCurrency == currencyType && addValue > 0)
        {
            int target = currentValue + addValue;
            _delayTweener?.Kill();
            _runTweener?.Kill();

            float delay = addValue > 0 ? _delayUpdate : 0f;
            _delayTweener = DOVirtual.DelayedCall(delay, () =>
            {
                int from = currentValue;
                _runTweener = DOTween.To(() => from, v =>
                {
                    // v là số nguyên đang tween
                    valueText.text = textConvert.Replace("{value}", v.ToString("n0"));
                }, target, _runUpdate).SetEase(Ease.OutCubic);
            });
        }
        else if (displayCurrency == currencyType)
        {
            int newValue = currentValue + addValue;
            valueText.text = textConvert.Replace("{value}", $"{newValue:#,##0,00}");
        }
    }
}