using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PopitStep : StepBase
{
    [Header("Raycast")]
    [SerializeField] private LayerMask _layerCase = ~0;   // layer chứa hạt popit
    [SerializeField] private float _rayMaxDistance = 5f;

    [Header("Completion")]
    [Range(0f, 1f)]
    [SerializeField] private float _completePercent = 1f; // 1 = tất cả hạt
    private int _needToComplete;

    [Header("Spine (finger)")]
    [SerializeField] private FingerSpine _spine;
    [SerializeField] private float _fingerMoveSpeed = 14f;
    [SerializeField] private Vector3 _fingerWorldOffset = new Vector3(0, 0, 0.01f);

    [Header("Tutorial config")]
    [SerializeField] private bool _enableTutorial = true;   // cho phép tutorial
    [SerializeField] private bool _firstTimeOnly = true;    // chỉ chạy lần đầu
    [SerializeField] private int _tutorialPops = 3;         // số hạt demo
    [SerializeField] private float _delayBefore = 0.4f;     // trễ trước khi bắt đầu
    [SerializeField] private float _betweenTaps = 0.35f;    // trễ giữa các lần tap

    [Header("Idle trigger (auto run tutorial)")]
    [SerializeField] private bool _tutorialOnIdle = true;   // tự chạy khi idle
    [SerializeField, Range(1f, 30f)] private float _idleSeconds = 5f;
    [SerializeField] private float _cooldownAfterTutorial = 1.2f;
    [SerializeField] private bool _countUITouchAsInteraction = true;

    private const string PREF_TUTORIAL = "Popit_TutorialDone";

    private PhoneCase _phone;
    private Camera _mainCam;
    private List<PopItNode> _nodes = new List<PopItNode>();
    private int _totalPop;
    private bool _tutorialPlaying;
    private float _lastInteractTime;


    // ================= StepBase =================
    public override void SetUp(PhoneCase phoneCase)
    {
        _phone = phoneCase;
        _mainCam = Camera.main;

        // gom node & bật node
        _nodes.Clear();
        _nodes.AddRange(_phone.GetComponentsInChildren<PopItNode>(true));
        foreach (var n in _nodes) n.EnableNode(true);

        // đếm đã pop sẵn + ngưỡng hoàn thành
        _totalPop = 0;
        foreach (var n in _nodes) if (n.IsClicked) _totalPop++;
        _needToComplete = Mathf.Clamp(Mathf.CeilToInt(_nodes.Count * _completePercent), 1, int.MaxValue);

        StopAllCoroutines();
        StartCoroutine(CheckReset());
        StartCoroutine(IdleWatcher());

        _lastInteractTime = Time.time;

        // tutorial lúc bắt đầu (nếu có)
        if (_enableTutorial && _spine != null &&
           (!_firstTimeOnly || PlayerPrefs.GetInt(PREF_TUTORIAL, 0) == 0))
        {
            StartCoroutine(TutorialCo());
        }

        enabled = true;
    }

    public override void CompleteStep()
    {
        if (_firstTimeOnly) PlayerPrefs.SetInt(PREF_TUTORIAL, 1);

        enabled = false;

        // Nếu StepBase của bạn có callback hoàn thành thì gọi ở đây
        // base.CompleteStep();
    }

    public void OnReset()
    {
        StopAllCoroutines();

        foreach (var n in _nodes) { n.EnableNode(true); n.SwitchState(false); }
        _totalPop = 0;

        _tutorialPlaying = false;
        if (_spine) _spine.Show(false);


        _lastInteractTime = Time.time;
        StartCoroutine(IdleWatcher());
        enabled = true;
    }

    // ================= Input =================
    private void MarkInteraction() => _lastInteractTime = Time.time;

    private Ray GetTouchRay()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        return _mainCam.ScreenPointToRay(Input.mousePosition);
#else
        Vector2 pos = (Input.touchCount > 0) ? (Vector2)Input.GetTouch(0).position : (Vector2)Input.mousePosition;
        return _mainCam.ScreenPointToRay(pos);
#endif
    }

    private bool IsOverUIThisFrame()
    {
        if (EventSystem.current == null) return false;
#if UNITY_EDITOR || UNITY_STANDALONE
        return EventSystem.current.IsPointerOverGameObject();
#else
        return (Input.touchCount > 0) && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
#endif
    }

    private void TryPopNode()
    {
        // chặn khi tutorial đang chạy
        if (_tutorialPlaying) return;

        // bỏ qua pop nếu bấm lên UI
        if (IsOverUIThisFrame()) return;

        var ray = GetTouchRay();
        if (Physics.Raycast(ray, out var hit, _rayMaxDistance, _layerCase))
        {
            var node = hit.collider.GetComponent<PopItNode>() ?? hit.collider.GetComponentInParent<PopItNode>();
            if (node == null) return;

            bool next = !node.IsClicked;  // toggle
            node.SwitchState(next);
            AudioManager.Instance?.PlayPop();

            _totalPop += next ? 1 : -1;
            if (_totalPop < 0) _totalPop = 0;

            if (_totalPop >= _needToComplete) CompleteStep();
        }
    }

    void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0))
        {
            if (_countUITouchAsInteraction || !IsOverUIThisFrame()) MarkInteraction();
            TryPopNode();
        }
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            if (_countUITouchAsInteraction || !IsOverUIThisFrame()) MarkInteraction();
            TryPopNode();
        }
#endif
    }

    // ================= Idle watcher =================
    private IEnumerator IdleWatcher()
    {
        while (enabled)
        {
            if (_tutorialOnIdle && !_tutorialPlaying && (Time.time - _lastInteractTime) >= _idleSeconds)
            {
                _lastInteractTime = Time.time;           // reset để không spam
                if (_enableTutorial && _spine != null)   // có finger thì chạy
                {
                    yield return StartCoroutine(TutorialCo());
                    _lastInteractTime = Time.time + _cooldownAfterTutorial;
                }
            }
            yield return null;
        }
    }

    // ================= Tutorial =================
    private IEnumerator TutorialCo()
    {
        _tutorialPlaying = true;
        _lastInteractTime = Time.time;

        // chọn N node chưa pop
        var demoList = new List<PopItNode>();
        foreach (var n in _nodes)
        {
            if (!n.IsClicked) demoList.Add(n);
            if (demoList.Count >= _tutorialPops) break;
        }
        if (demoList.Count == 0)
        {
            _tutorialPlaying = false;
            yield break;
        }

        yield return new WaitForSeconds(_delayBefore);

        _spine.Show(true);
        _spine.PlayIdle();

        for (int i = 0; i < demoList.Count; i++)
        {
            var node = demoList[i];
            if (node == null) continue;

            var col = node.GetComponent<Collider>();
            Vector3 wpos = (col != null) ? col.bounds.center : node.transform.position;
            wpos += _fingerWorldOffset;

            // di chuyển finger tới node
            yield return _spine.MoveToWorld(_mainCam, wpos, _fingerMoveSpeed);

            // tap demo
            _spine.TapOnce();
            yield return new WaitForSeconds(0.12f);

            if (!node.IsClicked)
            {
                node.SwitchState(true);
                AudioManager.Instance?.PlayPop();
                _totalPop++;
                if (_totalPop >= _needToComplete)
                {
                    _spine.Show(false);
                    _tutorialPlaying = false;
                    CompleteStep();
                    yield break;
                }
            }

            yield return new WaitForSeconds(_betweenTaps);
        }

        _spine.Show(false);
        _tutorialPlaying = false;
        _lastInteractTime = Time.time + _cooldownAfterTutorial;
    }

    // Public API
    public void StartTutorialNow()
    {
        if (!_enableTutorial || _spine == null) return;
        if (_tutorialPlaying) return;
        StartCoroutine(TutorialCo());
    }

    public void SkipTutorial()
    {
        if (!_tutorialPlaying) return;
        StopAllCoroutines();
        _tutorialPlaying = false;
        _spine?.Show(false);
        StartCoroutine(IdleWatcher());
    }

    private IEnumerator CheckReset()
    {
        // placeholder, nếu bạn cần loop gì khác để reset
        while (enabled) yield return null;
    }
}
