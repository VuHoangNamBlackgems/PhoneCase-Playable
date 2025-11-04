using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class StepUnscrew : StepBase
{
    [Header("Refs")] [SerializeField] Camera worldCam;
    [SerializeField] Transform tool; // tô vít
    [SerializeField] Transform bitTip; // đầu vít (điểm chạm)
    [SerializeField] Animator toolAnim; // anim quay đầu vít (tuỳ chọn)
    [SerializeField] string spinBool = "Spin"; // bool để quay

    [Header("Targets")] [SerializeField] LayerMask screwMask; // layer của ốc
    [SerializeField] List<ScrewTarget> screws; // nếu để trống sẽ tự FindInChildren

    [Header("Feel")] [SerializeField] float approachOffset = 0.003f; // cách mặt ốc 3mm
    [SerializeField] float flyTime = 0.35f; // thời gian bay tới
    [SerializeField] Ease flyEase = Ease.OutCubic;

    bool busy;
    
    [Header("Tutorial")]
    [SerializeField] bool enableTutorialOnStart = true;
    [SerializeField] bool restrictClickToCurrent = true;    
    [SerializeField] TutorialPointerSpine pointer;          

    ScrewTarget tutorialCurrent;

    private void Start()
    {
        
    }

    public override void SetUp(PhoneCase phoneCase)
    {
        CameraController.instance.MoveCamera(CameraController.instance.MainCam.transform, CAMERA_POINT.SCREW_IN);
        phoneCase.SetupScrewPos(null);
        screws = phoneCase.ListScrewTarget;
        if (!worldCam) worldCam = Camera.main;
        if (screws == null || screws.Count == 0)
            screws = new List<ScrewTarget>(GetComponentsInChildren<ScrewTarget>(true));
        busy = false;
        
        if (enableTutorialOnStart)
        {
            var first = screws.FirstOrDefault(s => s && !s.removed);
            if (first)
            {
                tutorialCurrent = first;
                pointer?.Show(first.anchor);
            }
        }
        else
        {
            tutorialCurrent = null;
            pointer?.Hide();
        }
    }

    void MovePointer()
    {
        var screwTut = screws.FirstOrDefault(s => s && !s.removed);
        if (screwTut)
        {
            tutorialCurrent = screwTut;
            pointer?.Show(screwTut.anchor);
        }
    }
    

    public override void CompleteStep()
    {
        
    }


    void Update()
    {
        if (busy) return;
        
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0)) TryPick((Vector2)Input.mousePosition);
#else
        if (Input.touchCount>0 && Input.GetTouch(0).phase==TouchPhase.Began)
            TryPick(Input.GetTouch(0).position);
#endif
    }

    void TryPick(Vector2 screenPos)
    {
        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return;

        var ray = worldCam.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out var hit, 100f, screwMask, QueryTriggerInteraction.Collide))
            return;
        AudioManager.Instance.PlayPop();
        var screw = hit.collider.GetComponentInParent<ScrewTarget>();
        if (!screw || screw.removed) return;
        
        if (restrictClickToCurrent && tutorialCurrent && screw != tutorialCurrent)
        {
            pointer?.Nudge();
            return;
        }
        StartCoroutine(DoUnscrew(screw, hit)); 
    }
    
    IEnumerator DoUnscrew(ScrewTarget s, RaycastHit hit)
    {
        busy = true;
        CameraController.instance.MoveCamera(CameraController.instance.MainCam.transform, CAMERA_POINT.SCREW_OUT);
        pointer?.Hide();
        tool.gameObject.SetActive(true);
        // Hướng rút: normal bề mặt (thế giới)
        Vector3 contact = s.anchor.position;
        Vector3 outward = hit.normal.normalized;

        // Tool đứng cách bề mặt 1 chút, đầu bit khớp vào tâm
        Vector3 approach = contact + outward * approachOffset; // vị trí tool khi tiếp cận
        Quaternion look = Quaternion.LookRotation(-outward, s.anchor.forward); // mũi tool hướng vào bề mặt

        // Bay tới & canh hướng
        var seq = DOTween.Sequence();
        seq.Join(tool.DOMove(approach, flyTime).SetEase(flyEase));
        // Nếu bitTip không phải con của tool thì tween riêng; nếu là con thì KHÔNG tween bitTip
        if (bitTip && !bitTip.IsChildOf(tool))
            seq.Join(bitTip.DOMove(contact, flyTime).SetEase(flyEase));
        yield return seq.WaitForCompletion();

        // Bắt đầu quay tool
        bool useAnimatorSpin = toolAnim && !string.IsNullOrEmpty(spinBool);
        if (useAnimatorSpin) toolAnim.SetBool(spinBool, true);

        // (Fallback) Nếu không có animator, cho tool tự quay quanh trục local forward
        Tween toolSpinTween = null;
        if (!useAnimatorSpin)
        {
            // Quay đúng số vòng tương ứng với số vòng của ốc
            toolSpinTween = tool.DOLocalRotate(new Vector3(0, 360f * s.turns, 0f), s.duration, RotateMode.LocalAxisAdd)
                .SetEase(Ease.Linear);
        }

        // Vị trí đích sau khi rút
        Vector3 screwTargetPos = s.screwMesh.position + new Vector3(0, 0f, -1.2f) * s.pullUp;
        Vector3 toolTargetPos = approach + new Vector3(0, 0f, -1.2f) * s.pullUp;
        Vector3 tipTargetPos = contact + new Vector3(0, 0f, -1.2f) * s.pullUp;
        // Rút ốc + tool cùng lúc (mũi vẫn dính đầu ốc)
        var uns = DOTween.Sequence();
        
        // Xoay ốc quanh trục local Y (giữ như thiết kế của bạn)
        uns.Join(s.screwMesh.DOLocalRotate(new Vector3(0, 360f * s.turns, 0), s.duration, RotateMode.LocalAxisAdd)
            .SetEase(Ease.Linear));

        // Kéo ốc ra ngoài theo world-space
        uns.Join(s.screwMesh.DOMove(screwTargetPos, s.duration).SetEase(Ease.OutSine));

        // Kéo tool ra cùng ốc, giữ offset tiếp cận
        uns.Join(tool.DOMove(toolTargetPos, s.duration).SetEase(Ease.OutSine));
        AudioManager.Instance.PlayScrew();
        // Nếu bitTip không phải con của tool, kéo riêng để luôn “cắn” vào đầu ốc
        if (bitTip && !bitTip.IsChildOf(tool))
            uns.Join(bitTip.DOMove(tipTargetPos, s.duration).SetEase(Ease.OutSine));

        yield return uns.WaitForCompletion();

        if (screws.All(x => x == null || x.removed))
        {
            
        }
        else
        {
            CameraController.instance.MoveCamera(CameraController.instance.MainCam.transform, CAMERA_POINT.SCREW_IN);
        }

        // Tắt quay
        if (toolSpinTween != null) toolSpinTween.Kill(false);

        s.MarkRemoved();
        DOVirtual.DelayedCall(1f, () =>
        {
            MovePointer();
        });
        //(Optional) thả tool lùi nhẹ ra sau cho đẹp
        //yield return tool.DOMove(toolTargetPos + outward * 0.02f, 0.15f).SetEase(Ease.OutQuad).WaitForCompletion();
        tool.gameObject.SetActive(false);
        // Check xong hết chưa
        if (screws.All(x => x == null || x.removed))
        {
            pointer?.Hide();
            StepFlow.instance.Next();
        }
        busy = false;
    }
}