using System.Collections;
using Spine;
using Spine.Unity;
using UnityEngine;

public class FingerSpine : MonoBehaviour
{
    [Header("Spine")]
    public SkeletonAnimation skel;       // gán trong Inspector (hoặc để trống -> auto)
    public string idleAnim = "idle";
    public string tapAnim  = "tap";

    [Header("Move")]
    [SerializeField] float arriveDistance = 0.005f; // m, sai số dừng

    Spine.AnimationState state;
    Skeleton skeleton;

    void Awake()
    {
        if (!skel) skel = GetComponent<SkeletonAnimation>();
        if (!skel)
        {
            Debug.LogError("[FingerSpine] Missing SkeletonAnimation");
            enabled = false; return;
        }

        state     = skel.AnimationState;
        skeleton  = skel.Skeleton;

        state.Data.DefaultMix = 0.15f;
        if (!string.IsNullOrEmpty(idleAnim))
            state.SetAnimation(0, idleAnim, true);

        state.Event += OnSpineEvent;
    }

    void OnDestroy()
    {
        if (state != null) state.Event -= OnSpineEvent;
    }

    // ===== Controls used by PopitStep =====
    public void Show(bool on) => gameObject.SetActive(on);

    public void PlayIdle()
    {
        if (!string.IsNullOrEmpty(idleAnim))
            state.SetAnimation(0, idleAnim, true);
    }

    // overlay 1 lần tap rồi tự fade-out
    public void TapOnce(float mix = 0.15f, float hold = 0.02f)
    {
        if (string.IsNullOrEmpty(tapAnim)) return;
        var e = state.SetAnimation(1, tapAnim, false);
        e.MixDuration = mix;
        state.AddEmptyAnimation(1, hold, mix);
    }

    public Coroutine MoveToWorld(Camera cam, Vector3 worldPos, float speed)
    {
        return StartCoroutine(MoveToWorldCo(cam, worldPos, speed));
    }

    IEnumerator MoveToWorldCo(Camera cam, Vector3 worldPos, float speed)
    {
        float sqr = arriveDistance * arriveDistance;

        while ((transform.position - worldPos).sqrMagnitude > sqr)
        {
            transform.position = Vector3.MoveTowards(transform.position, worldPos, speed * Time.deltaTime);
            if (cam) FaceToCam(cam);
            yield return null;
        }
        transform.position = worldPos;
    }

    public void FaceToCam(Camera cam)
    {
        // luôn hướng “mặt” về camera
        transform.rotation = Quaternion.LookRotation(cam.transform.forward);
    }

    // ===== Optional: API gốc bạn từng dùng =====
    public void Play(string anim, bool loop = false, float mix = -1f)
    {
        var e = state.SetAnimation(0, anim, loop);
        if (mix >= 0) e.MixDuration = mix;
    }

    public void Queue(string anim, bool loop = false, float delay = 0f)
    {
        state.AddAnimation(0, anim, loop, delay);
    }

    public void Overlay(string anim, float hold = 0.0f, float mix = 0.2f)
    {
        var e = state.SetAnimation(1, anim, false);
        e.MixDuration = mix;
        state.AddEmptyAnimation(1, hold, mix);
    }

    public void SetSkin(string skin)
    {
        skeleton.SetSkin(skin);
        skeleton.SetSlotsToSetupPose();
        skel.LateUpdate();
    }

    public void SetAttachment(string slot, string attachment)
    {
        skeleton.SetAttachment(slot, attachment);
    }

    void OnSpineEvent(TrackEntry t, Spine.Event e)
    {
        // hook SFX/VFX nếu cần
        // Debug.Log("Spine event: " + e.Data.Name);
    }
}
