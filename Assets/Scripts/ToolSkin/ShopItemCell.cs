using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemCell : MonoBehaviour
{
    [Header("Refs")]
    public Image icon;
    public GameObject lockOverlay;   
    public GameObject unlockOverlay; 
    public GameObject selectMark;    
    public Button button;

    [Header("Idle FX")]
    public bool idlePulseWhenLocked = true;

    public string SkinId { get; private set; }
    bool _unlocked;

    void Reset()
    {
        button = GetComponent<Button>();
    }

    public void Setup(Sprite icon, bool unlocked, bool selected, System.Action onClick)
    {
        this.icon.sprite = icon;
        this._unlocked = unlocked;

        lockOverlay.SetActive(!unlocked);
        unlockOverlay.SetActive(unlocked);
        selectMark.SetActive(selected);
        SkinId = icon ? icon.name : "";

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke());

        PlayIdle(unlocked);
    }

    void PlayIdle(bool unlocked)
    {
        transform.DOKill();
        transform.localScale = Vector3.one;

        if (!unlocked && idlePulseWhenLocked)
        {
            transform.DOScale(1.06f, 0.6f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }
    }

    public void SetSelected(bool selected)
    {
        selectMark.SetActive(selected);
        if (selected)
        {
            transform.DOKill();
            transform.DOPunchScale(Vector3.one * 0.12f, 0.2f, 10, 0.9f);
        }
    }

    public void PlayLockedNudge()
    {
        transform.DOKill();
        transform.DOShakePosition(0.2f, strength: new Vector3(10, 0, 0), vibrato: 30)
            .OnComplete(() => PlayIdle(_unlocked));
    }

    public void PlayUnlockFX()
    {
        _unlocked = true;
        lockOverlay.SetActive(false);
        transform.DOKill();
        transform.localScale = Vector3.one * 1.2f;
        transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
    }
}