// StepFlow.cs

using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class StepFlow : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PhoneCase phoneCase;
    [SerializeField] EmojiEndStep emojiEndStep;
    [Header("Steps")]
    [SerializeField] private List<StepBase> steps = new List<StepBase>();
    [SerializeField] private List<StepBase> allsteps = new List<StepBase>();
    [SerializeField] private bool deactivateInactiveSteps = true;
    [SerializeField] private int startIndex = 0;

    public int CurrentIndex { get; private set; } = -1;
    public PhoneCase PhoneCase
    {
        get => phoneCase;
        set => phoneCase = value;
    }

    public List<StepBase> Steps
    {
        get => steps;
        set => steps = value;
    }
    
    public List<StepBase> AllSteps
    {
        get => allsteps;
        set => allsteps = value;
    }

    public StepBase Current =>
        (CurrentIndex >= 0 && CurrentIndex < steps.Count) ? steps[CurrentIndex] : null;

    [Header("Events")]
    public UnityEvent<int, StepBase> onStepStarted;
    public UnityEvent<int, StepBase> onStepCompleted;
    public UnityEvent onAllStepsCompleted;

    protected  void Init()
    {
       // base.Init();
        if (deactivateInactiveSteps)
            foreach (var s in steps) if (s) s.gameObject.SetActive(false);
    }
    
    public static StepFlow instance;
    private void Awake()
    {
        Init();
        instance = this;

    }


    private void Start()
    {
        if (steps.Count == 0)
        {
            Debug.LogWarning("[StepFlow] Chưa có step nào.");
            return;
        }
        //JumpTo(0);
    }

    public void JumpTo(int index)
    {
        if (index < 0 || index >= steps.Count)
        {
            Debug.LogWarning("[StepFlow] Index không hợp lệ.");
            return;
        }
        ProgressionStep.instance.SetIndex(index);
        Debug.Log(index + " - " + steps[index]);
        if (deactivateInactiveSteps)
            for (int i = 0; i < steps.Count; i++)
                if (steps[i]) steps[i].gameObject.SetActive(i == index);

        CurrentIndex = index;
        var step = Current;
        DOVirtual.DelayedCall(0.15f, () =>
        {
            step?.SetUp(phoneCase);
        });
        onStepStarted?.Invoke(CurrentIndex, step);
    }

    public void Next()
    {
        if (Current == null) return;
        Current.CompleteStep();
        UserTracking.LevelStepCleared();
        //CallAdsManager.ShowInter("complete_step");
        onStepCompleted?.Invoke(CurrentIndex, Current);

        int next = CurrentIndex + 1;
        if (next >= steps.Count)
        {
            if (deactivateInactiveSteps && Current) Current.gameObject.SetActive(false);
            onAllStepsCompleted?.Invoke();
            return;
        }
        emojiEndStep.gameObject.SetActive(true);
        emojiEndStep.ShowEmojiHappy();
        DOVirtual.DelayedCall(2f, () =>
        {
            JumpTo(next);
        });
    }

    public void AddStep(StepBase step)
    {
        steps.Add(step);
    }
}

[Serializable]
public enum STEP
{
    Spray = 0,
    Colorful = 1,
    Acrylic = 2,
    Glue = 3,
    Blur = 4,
    Glitter = 5,
    Sticker2D = 6,
    Sticker3D = 7,
    PhoneStrap = 8,
    ChooseCase = 9,
    Popit = 10,
    Halloween = 11,
    PeelGlass = 12,
    ScrewOut = 13,
    ScreenOut = 14,
    RemoveScreen = 15,
    ChangeScreen = 16,
    GlueScreen = 17,
}
