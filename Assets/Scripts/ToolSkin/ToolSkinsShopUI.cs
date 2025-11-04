using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ToolSkinsShopUI : MonoBehaviour
{
    [Header("Config (SO)")]
    public ToolShopConfig config;

    [Header("Pricing")]
    [SerializeField] private int unlockPrice = 500;

    [Header("Popup Root")]
    [SerializeField] private CanvasGroup root;          
    [SerializeField] private RectTransform panel;       
    [SerializeField] private Image dimBg;               

    [Header("Popup Anim")]
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool timeScaleIndependent = true;
    [SerializeField] private float showDuration = 0.25f;
    [SerializeField] private float hideDuration = 0.18f;
    [SerializeField] private Ease showEase = Ease.OutBack;
    [SerializeField] private Ease hideEase = Ease.InBack;
    [SerializeField] private float startScale = 0.82f;  // scale bắt đầu
    [SerializeField] private float overshootScale = 1.06f; // nhẹ để pop

    [Header("Tabs")]
    public Toggle tabGlue;
    public Toggle tabDryer;
    public Toggle tabTable;

    [Header("Grid")]
    public RectTransform gridRoot;
    public GridLayoutGroup gridLayout;
    public ShopItemCell cellPrefab;

    [Header("Bottom Buttons")]
    public Button btnUnlockRandom;
    public TMP_Text txtUnlockPrice;
    public Button btnClaimX2;
    public TMP_Text txtClaim;

    [Header("FX")]
    public float showStagger = 0.05f;
    public Ease spawnEase = Ease.OutBack;

    ToolCategory _current = ToolCategory.Glue;
    readonly List<ShopItemCell> _cells = new List<ShopItemCell>();

    Sequence _seq;

    void OnEnable()
    {
        tabGlue.onValueChanged.AddListener(v => { if (v) SwitchTab(ToolCategory.Glue); });
        tabDryer.onValueChanged.AddListener(v => { if (v) SwitchTab(ToolCategory.Dryer); });
        tabTable.onValueChanged.AddListener(v => { if (v) SwitchTab(ToolCategory.Table); });

        switch (_current)
        {
            case ToolCategory.Glue:  tabGlue.isOn  = true; break;
            case ToolCategory.Dryer: tabDryer.isOn = true; break;
            case ToolCategory.Table: tabTable.isOn = true; break;
        }

       // btnUnlockRandom.onClick.AddListener(OnUnlockRandom);
        btnClaimX2.onClick.AddListener(OnClaimX2);

       // Rebuild();

        if (playOnEnable) PlayShow();
    }

    void OnDisable()
    {
        btnUnlockRandom.onClick.RemoveAllListeners();
        btnClaimX2.onClick.RemoveAllListeners();
        tabGlue.onValueChanged.RemoveAllListeners();
        tabDryer.onValueChanged.RemoveAllListeners();
        tabTable.onValueChanged.RemoveAllListeners();

        KillTweens();
    }

    // --------------------- Popup Animations ---------------------

    public void PlayShow(bool instant = false)
    {
        if (!root || !panel) return;

        KillTweens();

        // setup initial
        root.alpha = instant ? 1f : 0f;
        root.interactable = false;
        root.blocksRaycasts = false;

        if (dimBg) dimBg.canvasRenderer.SetAlpha(instant ? 1f : 0f);

        panel.localScale = Vector3.one * (instant ? 1f : startScale);

        if (instant)
        {
            root.interactable = root.blocksRaycasts = true;
            return;
        }

        _seq = DOTween.Sequence().SetUpdate(timeScaleIndependent);

        if (dimBg)
            _seq.Join(dimBg.DOFade(1f, showDuration * 0.6f));

        _seq.Append(panel.DOScale(overshootScale, showDuration).SetEase(showEase));
        _seq.Join(root.DOFade(1f, showDuration));
        _seq.Append(panel.DOScale(1f, 0.1f));
        _seq.OnComplete(() =>
        {
            root.interactable = root.blocksRaycasts = true;
        });
    }

    public void PlayHide(Action onDone = null, bool deactivateOnDone = true)
    {
        if (!root || !panel) { onDone?.Invoke(); return; }

        KillTweens();
        root.interactable = root.blocksRaycasts = false;

        _seq = DOTween.Sequence().SetUpdate(timeScaleIndependent);

        if (dimBg)
            _seq.Join(dimBg.DOFade(0f, hideDuration * 0.7f));

        _seq.Join(root.DOFade(0f, hideDuration * 0.7f));
        _seq.Join(panel.DOScale(startScale, hideDuration).SetEase(hideEase));

        _seq.OnComplete(() =>
        {
            if (deactivateOnDone) gameObject.SetActive(false);
            onDone?.Invoke();
        });
    }

    void KillTweens()
    {
        if (_seq != null && _seq.IsActive()) _seq.Kill();
        panel?.DOKill();
        root?.DOKill();
        dimBg?.DOKill();
    }

    // --------------------- Existing logic ---------------------

    void SwitchTab(ToolCategory cat)
    {
        _current = cat;
     //   Rebuild();
    }

    CategoryDef CurrDef => config ? config.GetCategory(_current) : null;

    void Rebuild()
    {
        foreach (var c in _cells) if (c) Destroy(c.gameObject);
        _cells.Clear();

        var def = CurrDef;
        if (def == null)
        {
            txtUnlockPrice.text = "-";
            txtClaim.text = "Claim x2";
            return;
        }
        txtUnlockPrice.text = $"Unlock Random <sprite=0> {unlockPrice}";

        for (int i = 0; i < def.skins.Count; i++)
        {
            var skin = def.skins[i];
            var cell = Instantiate(cellPrefab, gridRoot);
            _cells.Add(cell);

            bool unlocked = UserGameData.IsUnlocked(_current, skin.id);
            string selectedId = UserGameData.GetSelected(_current);
            bool selected = unlocked && selectedId == skin.id;

            cell.Setup(
                icon: skin.icon,
                unlocked: unlocked,
                selected: selected,
                onClick: () => OnCellClicked(skin.id, unlocked)
            );

            var t = (RectTransform)cell.transform;
            t.localScale = Vector3.one * 0.2f;
            t.DOScale(1f, 0.25f).SetDelay(i * showStagger).SetEase(spawnEase);
        }

        if (!string.IsNullOrEmpty(UserGameData.GetSelected(_current)))
            return;

        var firstUnlocked = def.skins.FirstOrDefault(s => UserGameData.IsUnlocked(_current, s.id));
        if (firstUnlocked != null)
        {
            UserGameData.SetSelected(_current, firstUnlocked.id);
            MarkSelection(firstUnlocked.id);
        }
    }

    void OnCellClicked(string skinId, bool unlocked)
    {
        if (!unlocked)
        {
            var cell = _cells.First(c => c.SkinId == skinId);
            cell.PlayLockedNudge();
            return;
        }

        UserGameData.SetSelected(_current, skinId);
        MarkSelection(skinId);
    }

    void MarkSelection(string skinId)
    {
        foreach (var c in _cells) c.SetSelected(c.SkinId == skinId);
    }

    void OnUnlockRandom()
    {
        var def = CurrDef;
        if (def == null) return;

        var locked = def.skins.Where(s => !UserGameData.IsUnlocked(_current, s.id)).ToList();
        if (locked.Count == 0) {/* LogHelper.LogYellow("All items unlocked!");*/ return; }

        int balance = UserInventory.GetCurrencyValue(CurrencyType.CASH);
        if (balance < unlockPrice)
        {
         //   LogHelper.LogYellow("Not enough coins!");
            return;
        }
        UserInventory.ChangeCurrency(CurrencyType.CASH, -unlockPrice, true, true);

        var pick = locked[UnityEngine.Random.Range(0, locked.Count)];
        UserGameData.SetUnlocked(_current, pick.id, true);

        var cell = _cells.First(c => c.SkinId == pick.id);
        cell.PlayUnlockFX();

        UserGameData.SetSelected(_current, pick.id);
        MarkSelection(pick.id);
    }

    void OnClaimX2()
    {
        var def = CurrDef;
        if (def == null) return;
        UserInventory.ChangeCurrency(CurrencyType.CASH, 500, true, true);
    }
}
