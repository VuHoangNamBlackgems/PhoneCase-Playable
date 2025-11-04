using System;
using DG.Tweening;
using UnityEngine;

public class RemoveScreenStep : StepBase
{
    [Header("Drag Refs")]
    public Camera cam;
    public Transform target;      
    public Transform dragPlane;   
    public Collider handle;
    public Transform targetOut;
    public Transform screenBroken;
    public Transform screenBrokenPos;
    [Header("Detach check")]
    public Transform socket;      
    public float detachRadius = 0.03f; 
    public bool onlyWhileDragging = true;
    public TutorialPointerSpine tutorialPointer;
    
    bool dragging, done;

    public override void SetUp(PhoneCase phoneCase)
    {
        if (!cam) cam = Camera.main;
        if (!target) target = transform;
        if (!dragPlane) dragPlane = target;
        target = phoneCase.TargetCable;
        dragPlane = phoneCase.TargetCable;
        handle = phoneCase.SphereCable.GetComponent<Collider>();
        targetOut = phoneCase.TargetOutCable;
        screenBroken = phoneCase.ScreenBroken;
        screenBrokenPos = phoneCase.ScreenBrokenPosOut;
        socket = phoneCase.Socket;
        tutorialPointer.Show(target);
    }

    void Update()
    {
        if (!done && !dragging && Pressed() && HitHandle())
        {
            tutorialPointer.Hide();
            dragging = true;
        }
        if (!done && dragging && Released()) dragging = false;

        if (!done && dragging && RayToPlane(out var p)) target.position = p;

        // ---- ra khỏi socket thì complete ----
        if (!done && socket && Released())
        {
            if ((onlyWhileDragging) || !onlyWhileDragging)
            {
                if (Vector3.Distance(target.position, socket.position) > detachRadius)
                    CompleteStep();
            }
        }
    }

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
        if (!done)
        {
            tutorialPointer.Hide();
            target.position = targetOut.position;
            screenBroken.DOMove(screenBrokenPos.position, 1.3f).OnComplete(()=> StepFlow.instance.Next());
        }
        done = true;
        Debug.Log("Remove");
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (socket)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(socket.position, detachRadius);
        }
    }
#endif
}
