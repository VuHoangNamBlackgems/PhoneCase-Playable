using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class PeelGlassStep : StepBase
{
    [Header("Gesture")]
    [SerializeField] float minDragCm = 1.5f;                 // vuốt đủ dài
    [Range(0,89)] [SerializeField] float angleToleranceDeg = 25f; // lệch tối đa so với hướng lên
    [SerializeField] bool oneShot = true;

    [Header("Animator")]
    [SerializeField] Animator glassAnimator;
    [SerializeField] string triggerName = "GlassOut";
    [SerializeField] string outStateName = "Out";
    [SerializeField] float completeDelay = 0.05f;

    [Header("Callbacks")]
    [SerializeField] UnityEvent onAction;

    [Header("Tutorial")]
    [SerializeField] Animator tutorialAnimator;
    [SerializeField] float tutorialShowDelay = 0.8f;
    bool tutorialVisible;
    float lastInteractTime;

    // runtime
    int activePointer = int.MinValue;
    Vector2 startPos;
    bool fired;
    bool tutRunning = false; // giữ nguyên nếu có logic ngoài
    bool tutEnd = false;     // giữ nguyên nếu có logic ngoài
    bool isClick = false;

    private void Start()
    {
        activePointer = int.MinValue;
        fired = false;
        enabled = true;
    }

    public override void SetUp(PhoneCase phoneCase)
    {
        glassAnimator = phoneCase.GlassAnimator;
        CameraController.instance.MoveCamera(CameraController.instance.MainCam.transform, CAMERA_POINT.ONTABLE);
        activePointer = int.MinValue;
        fired = false;
        enabled = true;

        lastInteractTime = Time.unscaledTime;
        HideTutorial();
    }

    public override void CompleteStep()
    {
        PlayOut();
    }

    void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0)) TryBegin(-1, (Vector2)Input.mousePosition);
        if (activePointer == -1 && Input.GetMouseButton(0))  CheckDrag((Vector2)Input.mousePosition);
        if (activePointer == -1 && Input.GetMouseButtonUp(0))
        {
            isClick = false;
            lastInteractTime = Time.unscaledTime;
            activePointer = int.MinValue;
        }
#else
        for (int i = 0; i < Input.touchCount; i++)
        {
            var t = Input.GetTouch(i);
            if (t.phase == TouchPhase.Began) TryBegin(t.fingerId, t.position);
            if (t.fingerId == activePointer && (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary))
                CheckDrag(t.position);
            if (t.fingerId == activePointer && (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled))
            {
                isClick = false;
                lastInteractTime = Time.unscaledTime;
                activePointer = int.MinValue;
            }
        }
#endif

        // Tutorial logic
        if (isClick)
        {
            HideTutorial();
        }
        else
        {
            if (!tutRunning && !tutEnd && !fired)
            {
                if (Time.unscaledTime - lastInteractTime >= tutorialShowDelay)
                    ShowTutorialOnce();
                else
                    HideTutorial();
            }
        }
    }

    void TryBegin(int id, Vector2 pos)
    {
        if (activePointer != int.MinValue) return;

        // ĐÃ BỎ HOÀN TOÀN CHECK UI: luôn nhận touch/mouse
        isClick = true;
        activePointer = id;
        startPos = pos;
        PlayOut();
        lastInteractTime = Time.unscaledTime;
        HideTutorial();
    }

    void CheckDrag(Vector2 cur)
    {
        if (fired && oneShot) return;

        Vector2 delta = cur - startPos;
        if (delta.y <= 0f) return; // chỉ nhận kéo lên
        if (delta.magnitude < CmToPixels(minDragCm)) return;

        float cosTol = Mathf.Cos(angleToleranceDeg * Mathf.Deg2Rad);
        if (Vector2.Dot(delta.normalized, Vector2.up) >= cosTol)
        {
            CompleteStep();
        }
    }

    void PlayOut()
    {
        if (fired) return;        
        fired = true;
        if (tutorialAnimator) tutorialAnimator.gameObject.SetActive(false);
        onAction?.Invoke();
        if (glassAnimator)
        {
            AudioManager.Instance.PlayPeel();
            glassAnimator.ResetTrigger(triggerName);
            glassAnimator.SetTrigger(triggerName);
            StartCoroutine(WaitAnimThenComplete());
        }
    }

    System.Collections.IEnumerator WaitAnimThenComplete()
    {
        var info = glassAnimator.GetCurrentAnimatorStateInfo(0);
        while (!info.IsName(outStateName))
        {
            yield return null;
            info = glassAnimator.GetCurrentAnimatorStateInfo(0);
        }
        while (info.IsName(outStateName) && info.normalizedTime < 1f)
        {
            yield return null;
            info = glassAnimator.GetCurrentAnimatorStateInfo(0);
        }
        if (completeDelay > 0f) yield return new WaitForSeconds(completeDelay);
        AudioManager.Instance.StopPeel();
        glassAnimator.gameObject.SetActive(false);
        StepFlow.instance.Next();
    }

    float CmToPixels(float cm)
    {
        float dpi = Screen.dpi;
        if (dpi <= 0f) dpi = 160f;
        return cm * 0.3937008f * dpi;
    }

    void ShowTutorialOnce()
    {
        if (tutorialAnimator == null || tutorialVisible) return;
        tutorialAnimator.gameObject.SetActive(true);
        tutorialAnimator.Play("Tutorial", 0, 0f);
        tutorialVisible = true;
    }

    void HideTutorial()
    {
        if (tutorialAnimator == null || !tutorialVisible) return;
        tutorialAnimator.gameObject.SetActive(false);
        tutorialVisible = false;
    }
}
