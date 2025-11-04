using UnityEngine;
using DG.Tweening;
using Spine.Unity;

public class TutorialPointerSpine : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;
    public SkeletonAnimation spine;           // SkeletonAnimation (world-space)
    public string loopAnim = "animation";     // tên anim vòng lặp trong Spine

    [Header("Follow")]
    public Transform target;                  // ốc/anchor cần chỉ
    public Vector3 offsetLocal = new Vector3(0, 0.01f, 0);
    public bool offsetInTargetSpace = true;   // offset theo local của target
    public float followLerp = 20f;

    [Header("Billboard + Size")]
    public bool billboard = true;             // luôn quay mặt về camera
    public bool constantScreenSize = true;    // giữ size ổn định theo khoảng cách
    public float referenceDistance = 1.5f;    // khoảng cách chuẩn
    public float scaleAtReference = 0.1f;     // scale ở khoảng cách chuẩn

    [Header("Feedback")]
    public float nudgeScale = 1.15f;          // đẩy nhẹ khi click sai
    public float nudgeTime = 0.12f;

    Tween _nudge;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!spine) spine = GetComponent<SkeletonAnimation>();
        gameObject.SetActive(false);
    }

    public void Show(Transform t)
    {
        target = t;
        gameObject.SetActive(true);
        if (spine) spine.AnimationState.SetAnimation(0, loopAnim, true);
        UpdateNow(true);
    }

    public void MoveTo(Transform t, bool instant = true)
    {
        target = t;
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        UpdateNow(instant);
    }

    public void Hide()
    {
        target = null;
        gameObject.SetActive(false);
    }

    public void Nudge()
    {
        _nudge?.Kill();
        transform.localScale = Vector3.one * ComputeScale();
        _nudge = transform.DOPunchScale(Vector3.one * (nudgeScale - 1f), nudgeTime, 1, 0.5f);
    }

    void LateUpdate()
    {
        if (!target) return;
        UpdateNow(false);
    }

    void UpdateNow(bool instant)
    {
        // vị trí theo target + offset
        Vector3 worldPos = target.position + (offsetInTargetSpace ? target.TransformVector(offsetLocal) : offsetLocal);
        transform.position = instant ? worldPos : Vector3.Lerp(transform.position, worldPos, Time.unscaledDeltaTime * followLerp);

        // quay mặt về camera
        if (billboard && cam)
            transform.rotation = Quaternion.LookRotation(cam.transform.forward, cam.transform.up);

        // giữ kích thước tương đối ổn định theo khoảng cách
        float s = ComputeScale();
        transform.localScale = Vector3.one * s;
    }

    float ComputeScale()
    {
        if (!constantScreenSize || !cam) return scaleAtReference;
        float d = Vector3.Distance(cam.transform.position, transform.position);
        if (referenceDistance < 0.0001f) referenceDistance = 1f;
        return scaleAtReference * (d / referenceDistance);
    }
}
