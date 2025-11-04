using System;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ScreenOutStep : StepBase
{
    [Header("Refs")]
    [SerializeField] Animator toolAnimator;
    [SerializeField] Camera worldCam;
    [SerializeField] Collider toolCollider;

    [Header("Tool Options")]
    [SerializeField] string stateName = "screentool";
    [SerializeField] float playSpeed = 1f; 
    [SerializeField] bool pressOnToolOnly = false;
    [SerializeField] bool ignoreUI = true;

    [Header("Gesture")]
    [SerializeField] float minDragCm = 1.5f;                 
    [Range(0,89)] [SerializeField] float angleToleranceDeg = 25f; 
    [SerializeField] bool oneShot = true;
    
    [SerializeField] Animator tut1Animator;
    [SerializeField] Animator tut2Animator;
    
    bool nextStep = false;
    bool initialized;
    int activePointer = int.MinValue;
    Vector2 startPos;
    bool fired;
    PhoneCase phoneCase;
    void OnEnable()
    {
        
    }

    public override void SetUp(PhoneCase phoneCase)
    {
        this.phoneCase = phoneCase;
        phoneCase.SetupHorizonPos(null);
        CameraController.instance.MoveCamera(CameraController.instance.MainCam.transform, CAMERA_POINT.ONTABLE);
        DOVirtual.DelayedCall(0.2f, () =>
        {
            tut1Animator.gameObject.SetActive(true);
            tut1Animator.Play("tut");
        });
        if (!toolAnimator) { enabled = false; return; }
        toolAnimator.gameObject.SetActive(true);
        toolAnimator.Play(stateName, 0, 0f);
        toolAnimator.updateMode = AnimatorUpdateMode.Normal;
        toolAnimator.speed = 0f;
        initialized = true;
    }

    void Update()
    {
        if (!initialized) return;
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
        
        
        // bắt đầu giữ
        if (PressedThisFrame())
        {
            if (ignoreUI && IsPointerOverUI()) return;

            if (pressOnToolOnly && toolCollider && worldCam)
            {
                var ray = worldCam.ScreenPointToRay(CurrentPointerPos());
                if (!toolCollider.Raycast(ray, out _, 100f)) return;
            }
            tut1Animator.gameObject.SetActive(false);
            AudioManager.Instance.PlayScreenOut();
            toolAnimator.speed = playSpeed; // chạy
        }

        // thả tay → dừng
        if (ReleasedThisFrame())
        {
            AudioManager.Instance.StopScreenOut();
            toolAnimator.speed = 0f; // pause tại frame hiện tại
        }

        // đang chạy thì kiểm tra hết clip → Flip & Complete
        if (toolAnimator.speed > 0f)
        {
            var info = toolAnimator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName(stateName) && !info.loop && info.normalizedTime >= 1f)
            {
                AudioManager.Instance.StopScreenOut();
                toolAnimator.speed = 0f;
                toolAnimator.gameObject.SetActive(false);
                nextStep = true;
                tut2Animator.gameObject.SetActive(true);
                tut2Animator.Play("Tutorial");
                activePointer = int.MinValue;
                fired = false;
                enabled = true;
            }
        }
    }

    bool PressedThisFrame()
    {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    bool ReleasedThisFrame()
    {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        return Input.touchCount > 0 &&
               (Input.GetTouch(0).phase == TouchPhase.Ended || Input.GetTouch(0).phase == TouchPhase.Canceled);
#else
        return Input.GetMouseButtonUp(0);
#endif
    }

    Vector2 CurrentPointerPos()
    {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        return Input.touchCount > 0 ? Input.GetTouch(0).position : Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }

    bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        if (Input.touchCount == 0) return false;
        return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
#else
        return EventSystem.current.IsPointerOverGameObject();
#endif
    }

    public override void CompleteStep()
    {
        toolAnimator.gameObject.SetActive(false);
    }
    
    void TryBegin(int id, Vector2 pos)
    {
        if (activePointer != int.MinValue) return;
        if (ignoreUI && EventSystem.current != null)
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (EventSystem.current.IsPointerOverGameObject()) return;
#else
            if (EventSystem.current.IsPointerOverGameObject(id)) return;
#endif
        }
        tut2Animator.gameObject.SetActive(false);
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

        if (Vector2.Dot(dir, Vector2.left) >= cosTol)
        {
            fired = true;                       
            activePointer = int.MinValue;

            if (phoneCase != null)
            {
                phoneCase.FlipOpen(true, null, () => CompleteStep());
                CameraController.instance.MoveCamera(CameraController.instance.MainCam.transform, CAMERA_POINT.SCREEN_OPEN);
            }
            
            StepFlow.instance.Next();
        }
    }

    float CmToPixels(float cm)
    {
        float dpi = Screen.dpi;
        if (dpi <= 0f) dpi = 160f;
        return cm * 0.3937008f * dpi;
    }
    
    void ShowTutorialOnce(Animator animator)
    {
        if (animator == null) return;
        animator.gameObject.SetActive(true);
        animator.Play("Tutorial", 0, 0f);
    }

    void HideTutorial(Animator tutorialAnimator)
    {
        if (tutorialAnimator == null) return;
        tutorialAnimator.gameObject.SetActive(false);
    }
}
