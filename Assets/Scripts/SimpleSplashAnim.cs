using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SimpleSplashAnim : MonoBehaviour
{
    [Header("Root & Logo")]
    [SerializeField] CanvasGroup root;
    [SerializeField] RectTransform logoGroup;
    [SerializeField] RectTransform phoneWobble;

    [Header("Loading Bar")]
    [SerializeField] Image barFill;
    [SerializeField] TMP_Text loadingText;

    [Header("Options")]
    [SerializeField] bool playOnEnable = true;
    [SerializeField] bool autoSimulate = true;
    [SerializeField] float simulateDuration = 2.6f; // có thể ngắn hơn 4s, phần còn lại sẽ chờ đủ
    [SerializeField] private Transform adsPos;
    [SerializeField] private Camera cam;

    [Header("Timing")]
    [SerializeField] float minShowSeconds = 4f;   // HIỂN THỊ TỐI THIỂU ~4s
    [SerializeField] bool useUnscaledTime = true; // tránh bị pause ảnh hưởng
    [SerializeField] float fadeIn = 0.35f;
    [SerializeField] float fadeOut = 0.25f;
    public Action onCompleted;

    Sequence seq;
    Coroutine dotCo, simCo, finishWaitCo;
    float progress;
    bool finished;            // đã fade xong
    bool requestedComplete;   // đã yêu cầu complete (đang chờ đủ 4s)
    float startTime;

    bool loadingGameMode = false;

    public static SimpleSplashAnim instance;
    private void Awake()
    {
        instance = this;
    //    loadingGameMode = GameConfig.instance.InGameConfig.loadingGameMode;
    }

    void OnEnable()
    {

    }

    public void Play()
    {
  /*      if (loadingGameMode)
        {
            CallAdsManager.ShowONA("before_game_play");
        }*/

        KillTweens();

        finished = false;
        requestedComplete = false;
        progress = 0f;
        startTime = useUnscaledTime ? Time.unscaledTime : Time.time;

        if (barFill) barFill.fillAmount = 0f;

        if (root)
        {
            root.alpha = 0f;
            root.DOFade(1f, fadeIn);
        }

        // Logo pop + lơ lửng
        if (logoGroup)
        {
            logoGroup.localScale = Vector3.one * 0.92f;
            seq = DOTween.Sequence()
                .Append(logoGroup.DOScale(1f, 0.45f).SetEase(Ease.OutBack))
                .Join(logoGroup.DOLocalMoveY(logoGroup.localPosition.y + 10f, 1.6f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo));
        }


        // Điện thoại lắc nhẹ
        if (phoneWobble)
        {
            phoneWobble.DOLocalRotate(new Vector3(0, 0, 8f), 0.8f)
                       .SetEase(Ease.InOutSine)
                       .SetLoops(-1, LoopType.Yoyo);
        }

        // Loading… nhấp nháy
        if (loadingText) dotCo = StartCoroutine(AnimateDots());

        // Giả lập tiến độ
        if (autoSimulate) simCo = StartCoroutine(SimulateProgress());
    }

    /// <summary>Gọi nếu có tiến độ thật (AsyncOperation.progress)</summary>
    public void SetProgress(float p)
    {
        if (finished) return;
        progress = Mathf.Clamp01(p);
        if (barFill) barFill.fillAmount = progress;

        if (progress >= 1f)
            RequestComplete(); // không tắt ngay, chờ đủ 4s
    }

    IEnumerator SimulateProgress()
    {
        float t = 0f;
        while (t < simulateDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / simulateDuration);
            SetProgress(Mathf.Min(0.99f, k));
            yield return null;
        }

        // buffer nhẹ cho cảm giác thật
        yield return new WaitForSeconds(0.2f);
        SetProgress(1f); // sẽ gọi RequestComplete()
    }

    IEnumerator AnimateDots()
    {
        string baseText = "Loading";
        int i = 0;
        while (true)
        {
            i = (i + 1) % 4; // "", ".", "..", "..."
            loadingText.text = baseText + new string('.', i);
            loadingText.transform.DOPunchScale(Vector3.one * 0.05f, 0.2f, 6, 0.8f);
            yield return new WaitForSeconds(0.25f);
        }
    }

    // YÊU CẦU KẾT THÚC: chờ đủ minShowSeconds rồi mới fade out
    void RequestComplete()
    {
        if (requestedComplete || finished) return;
        requestedComplete = true;

        float now = useUnscaledTime ? Time.unscaledTime : Time.time;
        float elapsed = now - startTime;
        float wait = Mathf.Max(0f, minShowSeconds - elapsed);

        if (finishWaitCo != null) StopCoroutine(finishWaitCo);
        finishWaitCo = StartCoroutine(FinishAfter(wait));
    }

    IEnumerator FinishAfter(float delay)
    {
        if (delay > 0f)
        {
            if (useUnscaledTime) yield return new WaitForSecondsRealtime(delay);
            else yield return new WaitForSeconds(delay);
        }
        FinishNow();
    }

    void FinishNow()
    {
        if (finished) return;
        finished = true;

      //  CallAdsManager.HideMREC();

        DOTween.Kill(logoGroup);
        DOTween.Kill(phoneWobble);

        // dừng các coroutine phụ
        if (dotCo != null) { StopCoroutine(dotCo); dotCo = null; }
        if (simCo != null) { StopCoroutine(simCo); simCo = null; }

        // Fade out rồi callback
        if (root) root.DOFade(0f, fadeOut);
        onCompleted?.Invoke();
    }

    void KillTweens()
    {
        DOTween.Kill(logoGroup);
        DOTween.Kill(phoneWobble);
        if (dotCo != null) { StopCoroutine(dotCo); dotCo = null; }
        if (simCo != null) { StopCoroutine(simCo); simCo = null; }
        if (finishWaitCo != null) { StopCoroutine(finishWaitCo); finishWaitCo = null; }
    }

    void OnDisable() => KillTweens();
}
