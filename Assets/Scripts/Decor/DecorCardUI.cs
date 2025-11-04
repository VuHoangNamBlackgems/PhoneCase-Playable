using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DecorCardUI : MonoBehaviour
{
    [SerializeField] DecorType type;
    [SerializeField] private GameObject groupCash;
    [SerializeField] private GameObject groupAd;
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text levelText;   
    [SerializeField] TMP_Text costText;    
    [SerializeField] Button upgradeBtn;
    [SerializeField] UpgradeHintIcon upgradeHintIcon;
    [SerializeField] Sprite adsSprite;
    [SerializeField] Sprite cashSprite;

    private void Awake()
    {
    }

    void OnEnable()
    {
        Refresh();
        DecorManager.Instance.OnLevelChanged += OnChanged;
        UserInventory.CurrencyChangedHandler += OnCurrencyChanged;
    }
    void OnDisable()
    {
        if (DecorManager.Instance) DecorManager.Instance.OnLevelChanged -= OnChanged;
        UserInventory.CurrencyChangedHandler -= OnCurrencyChanged;
    }

    void OnChanged(DecorType t, int _) { if (t == type) Refresh(); }
    void OnCurrencyChanged(CurrencyType t, int __, int ___)
    {
        if (t == CurrencyType.CASH) Refresh();
    }

    public void OnClickUpgrade()
    {
        if (DecorManager.Instance.TryUpgrade(type)) Refresh();
    }

    void Refresh()
    {
        var man = DecorManager.Instance;
        int lv  = man.GetLevel(type);
        int max = man.MaxLevel(type);
        levelText.text = $"Level {lv + 1}";

        int nextCost = man.GetNextCost(type);
        bool maxed   = lv >= max - 1 || nextCost < 0;

        if (maxed)
        {
            costText.text = "MAX";
            upgradeBtn.interactable = false;
            upgradeBtn.gameObject.SetActive(false);
            if (groupCash) groupCash.SetActive(false);
            if (groupAd)   groupAd.SetActive(false);
            return;
        }
        
        costText.text = "<sprite=0> " + nextCost.ToString();

        int cash = UserInventory.GetCurrencyValue(CurrencyType.CASH);
        bool canAfford = cash >= nextCost;

        if (canAfford)
        {
            upgradeBtn.GetComponent<Image>().sprite = cashSprite;
            upgradeHintIcon.gameObject.SetActive(true);
        }
        else
        {
            upgradeBtn.GetComponent<Image>().sprite = adsSprite;
            upgradeHintIcon.gameObject.SetActive(false);
        }
        
        if (groupCash) groupCash.SetActive(canAfford);
        if (groupAd)   groupAd.SetActive(!canAfford);
    }
}