using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NewFeatureUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] CanvasGroup root;
    [SerializeField] RectTransform banner;
    [SerializeField] RectTransform itemUnlock;
    [SerializeField] CanvasGroup tapToContinue;
    [SerializeField] private List<ToolRef> listTools;
    
    [Header("Name Pill")]
    [SerializeField] RectTransform namePill;
    [SerializeField] CanvasGroup nameGroup;
    [SerializeField] TMP_Text nameText;
    
    Sequence _intro;
    Tween _idleChar, _idleTap, _idleName;
    readonly List<Tween> _idleStars = new List<Tween>();

    Vector2 _bannerPos;
    Vector3 _charPos;
    Vector2 _namePos;

    Image itemUnlockImage => itemUnlock.GetComponent<Image>();
    
    void Awake()
    {
        _bannerPos = banner.anchoredPosition;
        _charPos   = itemUnlock.localPosition;
        _namePos   = namePill.anchoredPosition;
    }

    void OnDisable() => KillAll();

    public void SetUpUnlockItem(STEP tool)
    {
        var toolRef = listTools.Find(x => x.tool == tool);
        itemUnlockImage.sprite = toolRef?.sprite;
    }
    
    public void PlayIntro()
    {
        KillAll();
        root.alpha = 0f;
        banner.anchoredPosition = _bannerPos + new Vector2(0, 280);
        itemUnlock.localScale = Vector3.zero;

        tapToContinue.alpha = 0f;
        tapToContinue.transform.localScale = Vector3.one * 0.9f;

        nameGroup.alpha = 0f;
        namePill.localScale = Vector3.one * 0.6f;
        namePill.anchoredPosition = _namePos + new Vector2(0, -60f);
        if (nameText) nameText.maxVisibleCharacters = 0;

        _intro = DOTween.Sequence();

        _intro.Append(root.DOFade(1, 0.2f));

        _intro.Join(banner.DOAnchorPos(_bannerPos, 0.55f).SetEase(Ease.OutBack, 1.4f));

        _intro.Insert(0.12f, itemUnlock.DOScale(1f, 0.55f).SetEase(Ease.OutBack));

        _intro.Insert(0.28f, nameGroup.DOFade(1f, 0.25f));
        _intro.Insert(0.28f, namePill.DOAnchorPos(_namePos, 0.35f).SetEase(Ease.OutCubic));
        _intro.Insert(0.28f, namePill.DOScale(1.06f, 0.35f).SetEase(Ease.OutBack)
            .OnComplete(() => namePill.DOScale(1f, 0.15f)));
        _intro.InsertCallback(0.33f, () =>
        {
            if (!nameText) return;
            DOTween.To(
                () => nameText.maxVisibleCharacters,
                v  => nameText.maxVisibleCharacters = v,
                nameText.text.Length,
                0.40f
            ).SetEase(Ease.Linear);
        });

        _intro.Insert(0.45f, tapToContinue.DOFade(1, 0.25f));

        _intro.OnComplete(StartIdleLoops);
    }

    void StartIdleLoops()
    {
        _idleChar = itemUnlock.DOLocalMoveY(_charPos.y + 12f, 1.2f)
            .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);

        _idleTap = tapToContinue.transform.DOScale(1.06f, 0.6f)
            .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);

        _idleName = namePill.DOScale(1.03f, 1.0f)
            .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
        // mún “bob” nhẹ:
        // namePill.DOAnchorPosY(_namePos.y + 5f, 1.2f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
    }

    public void Hide(Action onDone = null)
    {
        KillIdlesOnly();

        DOTween.Sequence()
            .Append(tapToContinue.DOFade(0, 0.15f))
            .Join(nameGroup.DOFade(0, 0.15f))
            .Join(namePill.DOScale(0.9f, 0.15f))
            .Join(itemUnlock.DOScale(0.9f, 0.15f))
            .Join(banner.DOAnchorPos(_bannerPos + new Vector2(0, 220), 0.22f).SetEase(Ease.InQuad))
            .Join(root.DOFade(0, 0.22f))
            .OnComplete(() => { onDone?.Invoke(); gameObject.SetActive(false); });
    }

    public void SetName(string displayName)
    {
        if (!nameText) return;
        nameText.text = displayName;
        nameText.maxVisibleCharacters = 0;
    }

    void KillIdlesOnly()
    {
        _idleChar?.Kill();
        _idleTap?.Kill();
        _idleName?.Kill();
        foreach (var t in _idleStars) t?.Kill();
        _idleStars.Clear();
    }

    void KillAll()
    {
        _intro?.Kill();
        KillIdlesOnly();
    }

    public void SetIcon(Sprite icon)
    {
        itemUnlockImage.sprite = icon;
    }

    public void OnPopUpClosed()
    {
        root.alpha = 0f;
        gameObject.SetActive(false);
        LevelRewardsManager.instance.OnPopupClosed();
    }
}

[System.Serializable]
public class ToolRef
{
    public STEP tool;
    public Sprite sprite;
}
