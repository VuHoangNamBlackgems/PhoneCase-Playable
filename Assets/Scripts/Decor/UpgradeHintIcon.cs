using DG.Tweening;
using UnityEngine;

public class UpgradeHintIcon : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float scaleAmount = 1.15f;
    [SerializeField] private float scaleDuration = 0.6f;

    [SerializeField] private float moveY = 6f;
    [SerializeField] private float moveDuration = 0.8f;

    private Sequence animSeq;

    void OnEnable()
    {
        PlayAnimation();
    }

    void OnDisable()
    {
        StopAnimation();
    }

    void PlayAnimation()
    {
        if (animSeq != null) animSeq.Kill();

        RectTransform rt = transform as RectTransform;
        Vector3 originalPos = rt.anchoredPosition;

        animSeq = DOTween.Sequence();
        animSeq.Append(rt.DOScale(scaleAmount, scaleDuration).SetEase(Ease.InOutSine));
        animSeq.Join(rt.DOAnchorPosY(originalPos.y + moveY, moveDuration / 2).SetEase(Ease.InOutSine));
        animSeq.Append(rt.DOScale(1f, scaleDuration).SetEase(Ease.InOutSine));
        animSeq.Join(rt.DOAnchorPosY(originalPos.y, moveDuration / 2).SetEase(Ease.InOutSine));
        animSeq.SetLoops(-1, LoopType.Restart);
    }

    void StopAnimation()
    {
        if (animSeq != null) animSeq.Kill();
        transform.localScale = Vector3.one;
    }
}