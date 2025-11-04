using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class PhoneStrapStep : StepBase
{
    public Transform spawnItem;
    public PhoneStrapPreview phoneStrapPreviewPrefab;
    public List<PhoneStrapPreview> phoneStrapPreviews = new List<PhoneStrapPreview>();
    public Transform phoneStrapSlot;
    public GameObject currentPhoneStrap;

    [Header("Idle Pulse")]
    [SerializeField] private float doneIdleDelay = 3f; // thời gian đứng im trước khi nháy lại

    // runtime
    private bool _sprayedDuringDrag = false;  
    private bool _armedIdlePulse = false;     // chỉ bật sau khi có tương tác người dùng
    private bool _idlePulseActive = false;    
    private float _lastInteractionTime = 0f;  
    private Sequence _donePulseSeq;
    private Vector3 _btnDoneBaseScale = Vector3.one;

    private void Start()
    {
        Initialized();

        // Chuẩn scale gốc của nút Done
        if (_btnDone != null)
            _btnDoneBaseScale = _btnDone.localScale;
        else
            _btnDoneBaseScale = Vector3.one;

        // Khi mới vào step: không nháy, đợi đến khi có click spawn mới bắt đầu đếm idle
        _armedIdlePulse = false;
        _lastInteractionTime = Time.unscaledTime;
        StopDonePulse();
    }

    private void OnDisable()
    {
        StopDonePulse();
    }

    private void Update()
    {
        CheckIdlePulseTick();
    }
    
    public override void SetUp(PhoneCase phoneCase)
    {
        phoneStrapSlot = phoneCase.PhoneStrapSlot.transform;
        UnSelect();
    }

    public override void CompleteStep()
    {
        StopDonePulse();
    }

    public void Initialized()
    {
        phoneStrapPreviews.Clear();
       /* 
        var manager = ItemDataManager.Instance.listPhoneStrap;
        
        foreach (var data in manager)
            if (data.isUnlock && !UserGameData.IsItemPhoneStrapUnlocked(data.id))
                UserGameData.UnlockItemPhoneStrap(data.id);
        
        for (int i = 0; i < manager.Count; i++)
        {
            var phoneStrap = Instantiate(phoneStrapPreviewPrefab, spawnItem);
            phoneStrap.SetUp(manager[i], () => { PhoneStrap_OnClick(phoneStrap); });
            phoneStrap.SetUpUnlock(UserGameData.IsItemPhoneStrapUnlocked(phoneStrap.PhoneStrapData.id));
            phoneStrapPreviews.Add(phoneStrap);
        }*/
    }

    public void PhoneStrap_OnClick(PhoneStrapPreview p)
    {
        // === MỚI: đăng ký tương tác để dừng scale và reset timer 3s ===
        RegisterInteraction();

        if (!p.IsUnlock)
        {
          /*  CallAdsManager.ShowRewardVideo("reward", () =>
            {
                UserTracking.ItemPick(step.ToString(), p.PhoneStrapData.id);
                p.SetUpUnlock(true);
                UnSelect();
                p.Select(true);
                UserGameData.UnlockItemPhoneStrap(p.PhoneStrapData.id);
                if (currentPhoneStrap) Destroy(currentPhoneStrap);
                currentPhoneStrap = Instantiate(p.PhoneStrapData.phonestrapPrefab, phoneStrapSlot);
            });*/
            return;
        }

        UnSelect();
        p.Select(true);
        if (currentPhoneStrap) Destroy(currentPhoneStrap);
        currentPhoneStrap = Instantiate(p.PhoneStrapData.phonestrapPrefab, phoneStrapSlot);
    }

    public void UnSelect()
    {
        if(phoneStrapPreviews.Count < 0) return;
        foreach (var strap in phoneStrapPreviews)
            strap.Select(false);
    }

    // ================== Idle Pulse Control ==================
    private void RegisterInteraction()
    {
        StopDonePulse();
        _armedIdlePulse = true;
        _lastInteractionTime = Time.unscaledTime;
    }

    private void CheckIdlePulseTick()
    {
        if (_armedIdlePulse && !_idlePulseActive)
        {
            if (Time.unscaledTime - _lastInteractionTime >= doneIdleDelay)
                StartDonePulse();
        }
    }

    private void StartDonePulse()
    {
        if (_btnDone == null || _idlePulseActive) return;
        _idlePulseActive = true;

        // Nháy lặp vô hạn
        _donePulseSeq = DOTween.Sequence()
            .Append(_btnDone.DOScale(_btnDoneBaseScale * 1.12f, 0.3f).SetEase(Ease.InQuad))
            .Append(_btnDone.DOScale(_btnDoneBaseScale, 0.3f).SetEase(Ease.OutQuad))
            .SetLoops(-1, LoopType.Restart);
    }

    private void StopDonePulse()
    {
        if (_donePulseSeq != null)
        {
            _donePulseSeq.Kill();
            _donePulseSeq = null;
        }
        _idlePulseActive = false;
        if (_btnDone) _btnDone.localScale = _btnDoneBaseScale;
    }
}
