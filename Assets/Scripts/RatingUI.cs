using System;

using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class RatingUI : MonoBehaviour
{
     [Header("Refs")]
    [SerializeField] private CanvasGroup _root;         
    [SerializeField] private RectTransform _dialog;     
    [SerializeField] private RectTransform[] _stars;    
    [SerializeField] private RectTransform _btnLater;
    [SerializeField] private RectTransform _btnRate;

    [Header("Timings")]
    [SerializeField] private float _showDur = 0.28f;
    [SerializeField] private float _hideDur = 0.16f;
    [SerializeField] private float _starStepDelay = 0.04f;

    Sequence _seq;
    bool _showing;

    void OnDisable()
    {
        _seq?.Kill();
        _root?.DOKill();
        _dialog?.DOKill();
        _btnRate?.DOKill();
        _btnLater?.DOKill();
        foreach (var s in _stars) s?.DOKill();
        Hide();
    }

    private void OnEnable()
    {
        Show();
        _btnRate.GetComponent<Button>().interactable = false;
    }

    // Gọi để bật popup
    public void Show()
    {
        if(PlayerPrefs.GetInt("Tutorial") == 0) return;
        if(PlayerPrefs.GetInt("RATE") != 0) return;
        if (_showing) return;
        _showing = true;
        gameObject.SetActive(true);
        _dialog.gameObject.SetActive(true);
        _seq?.Kill();
        _root.alpha = 0f;
        _dialog.localScale = Vector3.one * 0.82f;

        // reset sao
        foreach (var s in _stars)
        {
            if (!s) continue;
            s.localScale = Vector3.one * 0.0f; // pop lần lượt
            s.DOKill();
        }

        _seq = DOTween.Sequence()
            .Join(_root.DOFade(1f, 0.18f))
            .Join(_dialog.DOScale(1f, _showDur).SetEase(Ease.OutBack));

        // pop 5 sao
        for (int i = 0; i < _stars.Length; i++)
        {
            var s = _stars[i];
            if (!s) continue;
            _seq.Insert(0.10f + i * _starStepDelay,
                s.DOScale(1f, 0.22f).SetEase(Ease.OutBack));
        }

        // nút Rate “thở”
        if (_btnRate)
        {
            _btnRate.localScale = Vector3.one;
            _btnRate.DOScale(1.06f, 0.7f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
        }

        // nút Later nhún nhẹ 1 lần
        if (_btnLater)
        {
            _btnLater.localScale = Vector3.one * 0.98f;
            _btnLater.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
        }
    }

    // Gọi để tắt popup
    public void Later()
    {
        Hide();
    }
    
    public void Hide(Action onComplete = null)
    {
        if (!_showing) return;
        _showing = false;

        _btnRate?.DOKill();
        foreach (var s in _stars) s?.DOKill();

        DOTween.Sequence()
            .Join(_root.DOFade(0f, _hideDur))
            .Join(_dialog.DOScale(0.92f, _hideDur))
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
                onComplete?.Invoke();
                GameUI.instance.Show(UIScreen.Home); });
    }

    private int star = 0;
    public void PlayStarSelect(int starIndex)
    {
        int idx = starIndex;
        if (idx >= _stars.Length) idx = starIndex - 1;     
        idx = Mathf.Clamp(idx, 0, _stars.Length - 1);     

        for (int i = 0; i < _stars.Length; i++)            
        {
            var tf = _stars[i];
            if (!tf) continue;

            bool active = i <= idx;
            if (tf.childCount > 0)
                tf.GetChild(0).gameObject.SetActive(active);   

            tf.DOKill();
            if (i == idx)                                      
                tf.DOPunchScale(Vector3.one * 0.18f, 0.18f, 10, 0.9f);
            else
                tf.localScale = Vector3.one;
        }
        _btnRate.GetComponent<Button>().interactable = true;
        star = idx + 1;
    }

    public void Rate()
    {
        if (!_btnRate) return;
        if (star > 3)
        {
            Debug.Log("review");
          //  InAppReviewManager.ShowReview();
        }
        //FirebaseEvent.LogEvent("rate_" + star);
        _btnRate.DOKill();
        PlayerPrefs.SetInt("RATE", 1);
        PlayerPrefs.Save();
        Sequence s = DOTween.Sequence();
        s.Append(_btnRate.DOScale(0.95f, 0.06f));
        s.Append(_btnRate.DOScale(1f, 0.14f).SetEase(Ease.OutBack));
        Hide();
    }
}
