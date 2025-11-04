using UnityEngine;
using DG.Tweening;
using TMPro;

public class LobbyButtonsIntro : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] RectTransform btnPlay;
    [SerializeField] CanvasGroup  btnPlayCg;
    [SerializeField] RectTransform btnSkip;
    [SerializeField] CanvasGroup  btnSkipCg;
    [SerializeField] RectTransform priceGroup;  // bubble/tags +300
    [SerializeField] TMP_Text     priceText;
    [SerializeField] int          bonusCoins = 300;

    [Header("Timing")]
    [SerializeField] float playIn = 0.35f;
    [SerializeField] float delayBeforeSkip = 0.12f;
    [SerializeField] float skipIn = 0.30f;
    [SerializeField] bool  autoPlayOnEnable = true;

    Sequence _seq;
    Vector3 _playPos0, _skipPos0;

    void Awake()
    {
        if (btnPlay) _playPos0 = btnPlay.anchoredPosition3D;
        if (btnSkip) _skipPos0 = btnSkip.anchoredPosition3D;
    }

    void OnEnable()
    {
        if (autoPlayOnEnable) PlayIntro();
    }

    void OnDisable() => _seq?.Kill();

    public void PlayIntro()
    {
        _seq?.Kill();
        Prep(btnPlay, btnPlayCg, _playPos0);
        Prep(btnSkip, btnSkipCg, _skipPos0);
        if (priceText) priceText.text = $"+{bonusCoins}";

        // BỎ QUA timeScale -> chạy được ngay cả khi Time.timeScale = 0
        _seq = DOTween.Sequence().SetUpdate(true);

        // 1) PLAY vào trước
        _seq.Append(ShowBtn(btnPlay, btnPlayCg, playIn).SetUpdate(true));
        if (priceGroup)
            _seq.Join(priceGroup
                .DOPunchScale(Vector3.one * 0.2f, 0.5f, 10, 0.85f)
                .SetDelay(0.05f)
                .SetUpdate(true));

        // 2) SKIP vào sau
        _seq.AppendInterval(delayBeforeSkip);
        _seq.Append(ShowBtn(btnSkip, btnSkipCg, skipIn).SetUpdate(true));
    }

    static void Prep(RectTransform rt, CanvasGroup cg, Vector3 pos0)
    {
        if (!rt) return;
        rt.localScale = Vector3.zero;
        rt.anchoredPosition3D = pos0 + new Vector3(0, -40, 0); // chuẩn bị trượt lên
        if (cg) { cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false; }
    }

    static Tween ShowBtn(RectTransform rt, CanvasGroup cg, float dur)
    {
        var toPos = rt.anchoredPosition3D + new Vector3(0, 40, 0);
        var s = DOTween.Sequence();
        s.Join(rt.DOScale(1f, dur).SetEase(Ease.OutBack));
        s.Join(rt.DOAnchorPos3D(toPos, dur).SetEase(Ease.OutCubic));
        if (cg) s.Join(cg.DOFade(1f, dur * 0.6f));
        s.AppendCallback(() => { if (cg) { cg.interactable = true; cg.blocksRaycasts = true; } });
        return s;
    }

    // Hiển thị ngay lập tức để kiểm tra nhanh trong Editor
    public void ShowInstant()
    {
        _seq?.Kill();
        if (btnPlay) { btnPlay.localScale = Vector3.one; btnPlay.anchoredPosition3D = _playPos0; if (btnPlayCg) { btnPlayCg.alpha = 1; btnPlayCg.interactable = true; btnPlayCg.blocksRaycasts = true; } }
        if (btnSkip) { btnSkip.localScale = Vector3.one; btnSkip.anchoredPosition3D = _skipPos0; if (btnSkipCg) { btnSkipCg.alpha = 1; btnSkipCg.interactable = true; btnSkipCg.blocksRaycasts = true; } }
    }
}
