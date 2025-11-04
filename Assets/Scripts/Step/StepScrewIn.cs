using System.Collections;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class StepScrewIn : StepBase
{
    [Header("Refs")]
    [SerializeField] Camera worldCam;
    [SerializeField] Transform tool;       // tô vít
    [SerializeField] Transform bitTip;     // đầu vít (điểm chạm)
    [SerializeField] Animator toolAnim;
    [SerializeField] string spinBool = "Spin";

    [Header("Targets")]
    [SerializeField] LayerMask screwMask;
    [SerializeField] ScrewTarget[] screws;

    [Header("Feel")]
    [SerializeField] float approachOffset = 0.003f; // tool đứng cách mặt ốc
    [SerializeField] float startNearOffset = 0.018f; // ốc nổi ra xa lỗ khi vào step
    [SerializeField] float flyTime = 0.30f;
    [SerializeField] Ease flyEase = Ease.OutCubic;

    bool busy;

    public override void SetUp(PhoneCase phoneCase)
    {
        CameraController.instance.MoveCamera(CameraController.instance.MainCam.transform, CAMERA_POINT.SCREW_IN);
        if (!worldCam) worldCam = Camera.main;

        if (screws == null || screws.Length == 0)
            screws = GetComponentsInChildren<ScrewTarget>(true);

        // Vừa vào: bật & đặt ốc nổi gần lỗ để người chơi bấm
        foreach (var s in screws.Where(x => x))
        {
        }

        if (tool) tool.gameObject.SetActive(false);
        busy = false;
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
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            TryPick(Input.GetTouch(0).position);
#endif
    }

    void TryPick(Vector2 screenPos)
    {
        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return;

        var ray = worldCam.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out var hit, 100f, screwMask, QueryTriggerInteraction.Collide))
            return;

        var s = hit.collider.GetComponentInParent<ScrewTarget>();
        if (!s || !s.removed) return;   // chỉ cho bấm ốc đang “ở ngoài”

        StartCoroutine(DoScrewIn(s, hit));
    }

    IEnumerator DoScrewIn(ScrewTarget s, RaycastHit hit)
    {
        busy = true;
        CameraController.instance.MoveCamera(CameraController.instance.MainCam.transform, CAMERA_POINT.SCREW_OUT);
        tool.gameObject.SetActive(true);
        Vector3 contact = s.anchor.position;

        var goSeq = DOTween.Sequence();
            goSeq.Join(tool.DOMove(s.transform.position, flyTime).SetEase(flyEase));
        yield return goSeq.WaitForCompletion();

        // Quay (Animator hay fallback Tween)
        if (s.audioSrc && s.screwInClip) s.audioSrc.PlayOneShot(s.screwInClip);

        var inSeq = DOTween.Sequence();
        inSeq.Join(s.screwMesh.DOLocalRotate(new Vector3(0, -360f * s.turns, 0), s.duration, RotateMode.LocalAxisAdd).SetEase(Ease.Linear));
        inSeq.Join(s.screwMesh.DOMove(contact, s.duration).SetEase(Ease.InSine));
        inSeq.Join(tool.DOMove(contact, s.duration).SetEase(Ease.InSine));
        yield return inSeq.WaitForCompletion();

        s.MarkInserted();               
        tool.gameObject.SetActive(false);
        CameraController.instance.MoveCamera(CameraController.instance.MainCam.transform, CAMERA_POINT.SCREW_IN);
        if (screws.All(x => x == null || !x.removed))
            StepFlow.instance.Next();

        busy = false;
    }
}
