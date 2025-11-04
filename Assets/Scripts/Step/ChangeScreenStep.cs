using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using UnityEngine.EventSystems;

public class ChangeScreenStep : StepBase
{
    [Header("Drag Refs")]
    public Camera cam;
    public Transform target;       
    public Transform dragPlane;    
    public Collider handle;        

    [Header("Attach to socket")]
    public Transform socket;               
    public float snapRadius = 0.035f;      
    public float attachDuration = 0.25f;
    public Vector3 rotOffsetEuler;         
    public bool parentToSocket = true;     
    public bool returnIfMiss = true;       
    public float returnDuration = 0.15f;
    
    [Header("Screen move on complete (optional)")]
    public Transform screenBroken;
    public Vector3 screenBrokenPosIn;

    bool dragging, done;
    Vector3 startPosTool; 
    Quaternion startRot;
    
    [Header("Gesture")]
    [SerializeField] bool ignoreUI = true;
    [SerializeField] float minDragCm = 1.5f;                 // vuốt đủ dài
    [Range(0,89)] [SerializeField] float angleToleranceDeg = 25f; // lệch tối đa so với hướng lên
    [SerializeField] bool oneShot = true;
    int activePointer = int.MinValue;
    
    [Header("Screw Refs")]
    [SerializeField] Camera worldCam;
    [SerializeField] Transform tool;       // tô vít

    [Header("Targets")]
    [SerializeField] LayerMask screwMask;
    [SerializeField] List<ScrewTarget> screws;

    [Header("Feel")]
    [SerializeField] float approachOffset = 0.003f; // tool đứng cách mặt ốc
    [SerializeField] float startNearOffset = 0.018f; // ốc nổi ra xa lỗ khi vào step
    [SerializeField] float flyTime = 0.30f;
    [SerializeField] Ease flyEase = Ease.OutCubic;
    
    [Header("Tutorial")]
    [SerializeField] bool enableTutorialOnStart = true;
    [SerializeField] bool restrictClickToCurrent = true;    
    [SerializeField] TutorialPointerSpine pointer;
    [SerializeField] TutorialPointerSpine pointer1;
    
    [SerializeField] private Animator tutRight;
    
    ScrewTarget tutorialCurrent;

    bool busy;
    bool fired;
    bool nextStep = false;
    bool nextStepCrew = false;
    PhoneCase phoneCase;
    Vector2 startPos;
    public override void SetUp(PhoneCase phoneCase)
    {
        this.phoneCase = phoneCase;
        target = phoneCase.TargetCable;
        dragPlane = phoneCase.TargetCable;
        handle = phoneCase.SphereCable.GetComponent<Collider>();
        socket = phoneCase.Socket;
        screenBroken = phoneCase.ScreenBroken;
        screenBrokenPosIn = phoneCase.ScreenBrokenPosIn;
        screws = phoneCase.ListScrewTarget;
        
        startPosTool = target.position;
        startRot = target.rotation;
        phoneCase.ChangeScreen();
        if (screenBroken)
            screenBroken.DOLocalMove(screenBrokenPosIn, 1.3f).OnComplete(() =>
            {
                pointer1?.Show(target);
            });
    }

    void Update()
    {
        if (done) return;

        if (nextStepCrew)
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetMouseButtonDown(0)) TryPick((Vector2)Input.mousePosition);
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            TryPick(Input.GetTouch(0).position);
#endif
            return;
        }

        
        if (nextStep)
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetMouseButtonDown(0)) TryBegin(-1, (Vector2)Input.mousePosition);
            if (activePointer == -1 && Input.GetMouseButton(0))  CheckDrag((Vector2)Input.mousePosition);
            if (activePointer == -1 && Input.GetMouseButtonUp(0)) activePointer = int.MinValue;
#else
        for (int i=0;i<Input.touchCount;i++)
        {
            var t = Input.GetTouch(i);
            if (t.phase == TouchPhase.Began) TryBegin(t.fingerId, t.position);
            if (t.fingerId == activePointer && (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary))
                CheckDrag(t.position);
            if (t.fingerId == activePointer && (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled))
                activePointer = int.MinValue;
        }
#endif
            return;
        }
        
        if (!dragging && Pressed())
        {
            if (ignoreUI && EventSystem.current != null)
            {
#if UNITY_EDITOR || UNITY_STANDALONE
                if (EventSystem.current.IsPointerOverGameObject()) return;
#else
        if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) return;
#endif
            }
            dragging = true;
            pointer1?.Hide(); 
        }

        if (dragging)
        {
            if (RayToPlane(out var p)) target.position = p;

            if (Released())
            {
                dragging = false;
                TryAttachOrReturn();
            }
        }
    }

    // ===== attach / return =====
    void TryAttachOrReturn()
    {
        if (!socket) return;

        if (Vector3.Distance(target.position, socket.position) <= snapRadius)
        {
            // gắn vào socket
            var tRot = socket.rotation * Quaternion.Euler(rotOffsetEuler);
            target.DOMove(socket.position, attachDuration).SetEase(Ease.OutCubic);
            target.DORotateQuaternion(tRot, attachDuration).SetEase(Ease.OutCubic)
                  .OnComplete(() =>
                  {
                      AudioManager.Instance.PlayPop();
                      //if (parentToSocket) target.SetParent(socket, true);
                      target.position = socket.position;
                      Next();
                  });
        }
        else
        {
            
        }
    }

    // ===== helpers =====
    bool HitHandle()
    {
        if (!handle) return true;
        var ray = cam.ScreenPointToRay(CurrentPos());
        return handle.Raycast(ray, out _, 100f);
    }

    bool RayToPlane(out Vector3 pos)
    {
        var ray = cam.ScreenPointToRay(CurrentPos());
        var plane = new Plane(dragPlane.forward, dragPlane.position);
        if (plane.Raycast(ray, out float t)) { pos = ray.GetPoint(t); return true; }
        pos = target.position; return false;
    }

    bool Pressed()
    {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }
    bool Released()
    {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        return Input.touchCount > 0 &&
               (Input.GetTouch(0).phase == TouchPhase.Ended || Input.GetTouch(0).phase == TouchPhase.Canceled);
#else
        return Input.GetMouseButtonUp(0);
#endif
    }
    Vector2 CurrentPos()
    {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        return Input.touchCount > 0 ? (Vector2)Input.GetTouch(0).position : Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }

    public override void CompleteStep()
    {
        if (done) return;
        done = true;
        
    }

    public void Next()
    {
        nextStep = true;
        tutRight.gameObject.SetActive(true);
        tutRight.Play("Tutorial");
    }

    public void NextScrewIn()
    {
        nextStepCrew = true;
        CameraController.instance.MoveCamera(CameraController.instance.MainCam.transform, CAMERA_POINT.SCREW_IN);
        phoneCase.SetupScrewPos(null);
        if (!worldCam) worldCam = Camera.main;

        if (screws == null || screws.Count == 0)
            screws = new List<ScrewTarget>(GetComponentsInChildren<ScrewTarget>(true));

        if (tool) tool.gameObject.SetActive(false);
        busy = false;

        foreach (var screw in screws)
            screw.gameObject.SetActive(true);
        
        if (enableTutorialOnStart)
        {
            var first = screws.FirstOrDefault(s => s && s.removed);
            if (first)
            {
                tutorialCurrent = first;
                pointer?.Show(first.transform);
            }
        }
        else
        {
            tutorialCurrent = null;
            pointer?.Hide();
        }
    }
    
    void TryBegin(int id, Vector2 pos)
    {
        tutRight.gameObject.SetActive(false);
        if (activePointer != int.MinValue) return;
        if (ignoreUI && EventSystem.current != null)
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (EventSystem.current.IsPointerOverGameObject()) return;
#else
            if (EventSystem.current.IsPointerOverGameObject(id)) return;
#endif
        }

        activePointer = id;
        startPos = pos;
    }

    void CheckDrag(Vector2 cur)
    {
        if (fired && oneShot) return;

        Vector2 delta = cur - startPos;
        float dist = delta.magnitude;
        if (dist < CmToPixels(minDragCm)) return;

        float cosTol = Mathf.Cos(angleToleranceDeg * Mathf.Deg2Rad);
        Vector2 dir = delta / dist;

        if (Vector2.Dot(dir, Vector2.right) >= cosTol)
        {
            fired = true;                       
            activePointer = int.MinValue;

            if (phoneCase != null)
            {
                phoneCase.FlipClose(true, true, () => NextScrewIn());
            }
        }
    }

    float CmToPixels(float cm)
    {
        float dpi = Screen.dpi;
        if (dpi <= 0f) dpi = 160f;
        return cm * 0.3937008f * dpi;
    }
    
    void TryPick(Vector2 screenPos)
    {
        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return;

        var ray = worldCam.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out var hit, 100f, screwMask, QueryTriggerInteraction.Collide))
            return;

        var s = hit.collider.GetComponentInParent<ScrewTarget>();
        if (!s || !s.removed) return;   // chỉ cho bấm ốc đang “ở ngoài”
        AudioManager.Instance.PlayPop();
        StartCoroutine(DoScrewIn(s, hit));
    }

    IEnumerator DoScrewIn(ScrewTarget s, RaycastHit hit)
    {
        busy = true;
        CameraController.instance.MoveCamera(CameraController.instance.MainCam.transform, CAMERA_POINT.SCREW_OUT);
        tool.gameObject.SetActive(true);
        Vector3 contact = s.anchor.position;
        pointer?.Hide();
        var goSeq = DOTween.Sequence();
        goSeq.Join(tool.DOMove(s.transform.position, flyTime).SetEase(flyEase));
        yield return goSeq.WaitForCompletion();

        // Quay (Animator hay fallback Tween)
        if (s.audioSrc && s.screwInClip) s.audioSrc.PlayOneShot(s.screwInClip);

        var inSeq = DOTween.Sequence();
        inSeq.Join(s.screwMesh.DOLocalRotate(new Vector3(0, -360f * s.turns, 0), s.duration, RotateMode.LocalAxisAdd).SetEase(Ease.Linear));
        inSeq.Join(tool.DOLocalRotate(new Vector3(0, -360f * s.turns, 0), s.duration, RotateMode.LocalAxisAdd).SetEase(Ease.Linear));
        inSeq.Join(s.screwMesh.DOMove(contact, s.duration).SetEase(Ease.InSine));
        inSeq.Join(tool.DOMove(contact, s.duration).SetEase(Ease.InSine));
        yield return inSeq.WaitForCompletion();

        s.MarkInserted();
        MovePointer();
        tool.gameObject.SetActive(false);
        CameraController.instance.MoveCamera(CameraController.instance.MainCam.transform, CAMERA_POINT.SCREW_IN);
        if (screws.All(x => x == null || !x.removed))
        {
            pointer?.Hide();
            StepFlow.instance.Next();
        }

        busy = false;
    }
    
    void MovePointer()
    {
        var screwTut = screws.FirstOrDefault(s => s && s.removed);
        if (screwTut)
        {
            tutorialCurrent = screwTut;
            pointer?.Show(screwTut.transform);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (socket)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(socket.position, snapRadius);
        }
    }
#endif
    
    
}
