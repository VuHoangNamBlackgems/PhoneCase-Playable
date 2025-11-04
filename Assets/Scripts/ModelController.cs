using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelController : MonoBehaviour
{
   public enum ANIMATION
    {
        IDLE = 0,
        MOVE = 1,
        WAVE = 2,
        SHOW_2_HAND = 3,
        INSPECT_PHONE = 4,
        CONFUSE = 5,
        SAD_WALK = 6,
        SAD_IDLE = 7
    }

    [Header("References")]
    [SerializeField] private Animator _animator;
    [SerializeField] private AudioSource _audio;
    [SerializeField] private Transform[] _phoneSlot;
    //[SerializeField] private PhoneStrap _phoneStrap;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1.6f;
    [SerializeField] private float rotateSpeed = 10f;
    [SerializeField] private float stopDistance = 0.02f;

    [Header("Face")]
    [SerializeField] private int faceLayerIndex = 1;
    [SerializeField] private float faceFade = 0.1f;

    [Header("Hold / IK")]
    [SerializeField] private string holdWeightParam = "HoldWeight";

    //================== Private state =================
    private PhoneCase _attachedPhone;
    private readonly Queue<Transform> _queueTarget = new Queue<Transform>();
    private Action _pathCallback;
    private bool _isMoving;
    private bool _isMoveOut;

    private const string P_IsMoving    = "IsMoving";
    private const string P_Is2Hand     = "Is2Hand";
    private const string P_IsSadMoving = "IsSadMoving";
    private const string T_Wave        = "Wave";
    private const string T_Inspect     = "Inspect";
    private const string T_Confuse     = "Confuse";
    private const string T_Saddle      = "Saddle";

    private void Awake()
    {
        if (_animator == null) _animator = GetComponent<Animator>();
        PlayAnimation(ANIMATION.IDLE);
    }

    public void EnableModel(bool isEnable) => gameObject.SetActive(isEnable);

    public void DeSpawn() => gameObject.SetActive(false);

    private void Start()
    {
    }

    public void PlayAnimation(ANIMATION a)
    {
        switch (a)
        {
            case ANIMATION.IDLE:
                SetMoving(false);
                SetSadMoving(false);
                SetTwoHand(false);
                break;

            case ANIMATION.MOVE:
                SetSadMoving(false);
                SetMoving(true);
                break;

            case ANIMATION.WAVE:
                Trigger(T_Wave);
                break;

            case ANIMATION.SHOW_2_HAND:
                SetTwoHand(true); 
                break;

            case ANIMATION.INSPECT_PHONE:
                Trigger(T_Inspect);
                break;

            case ANIMATION.CONFUSE:
                Trigger(T_Confuse);
                break;

            case ANIMATION.SAD_WALK:
                SetMoving(true);
                SetSadMoving(true);
                break;

            case ANIMATION.SAD_IDLE:
                SetMoving(false);
                SetSadMoving(true);
                break;
        }
    }

    public void PlaySkipVipAnimation() => Trigger(T_Saddle);

    public void PlayExpression(string stateName)
    {
        if (_animator == null) return;
        _animator.CrossFadeInFixedTime(stateName, faceFade, faceLayerIndex, 0f);
    }

    public void AttachPhone(PhoneCase phone, int slotIndex = 0)
    {
        _attachedPhone = phone;
        if (phone == null || _phoneSlot == null || _phoneSlot.Length == 0) return;

        var slot = _phoneSlot[Mathf.Clamp(slotIndex, 0, _phoneSlot.Length - 1)];
        phone.transform.SetParent(slot, false);
        phone.transform.localPosition = Vector3.zero;
        phone.transform.localRotation = Quaternion.identity;
        phone.transform.localScale    = Vector3.one;

        StartCoroutine(SetHoldWeight(1f, 0.2f));
    }

    public void AttachPhoneStrap(int index)
    {
        //if (_phoneStrap != null) _phoneStrap.Attach(index);
    }

    public void DetachPhone()
    {
        if (_attachedPhone == null) return;
        _attachedPhone.transform.SetParent(null, true);
        _attachedPhone = null;
        StartCoroutine(SetHoldWeight(0f, 0.2f));
    }

    public void CheckPlayAudio(bool isPlay = true)
    {
        if (_audio == null) return;
        if (isPlay)
        {
            if (!_audio.isPlaying) _audio.Play();
        }
        else
        {
            if (_audio.isPlaying) _audio.Stop();
        }
    }

    public void MoveCharacter(Transform target, Action callback = null)
    {
        if (target == null || _isMoving) return;
        StartCoroutine(CoMoveTo(target.position, callback));
    }

    public void FollowPath(Transform[] destination, Action callback = null, Action onDone = null)
    {
        _queueTarget.Clear();
        if (destination != null)
            foreach (var t in destination)
                if (t) _queueTarget.Enqueue(t);

        _pathCallback = callback;
        _isMoveOut = false;
        CheckPath();
    }

    public void MoveOut(Transform[] destination)
    {
        FollowPath(destination, () =>
        {
            _isMoveOut = true;
            DeSpawn();
        });
    }

    private void CheckPath()
    {
        if (_isMoving) return;

        if (_queueTarget.Count == 0)
        {
            _pathCallback?.Invoke();
            return;
        }

        var next = _queueTarget.Dequeue();
        MoveCharacter(next, CheckPath);
    }

    private IEnumerator CoMoveTo(Vector3 targetPos, Action onDone)
    {
        _isMoving = true;
        SetMoving(true);

        while (true)
        {
            Vector3 dir = targetPos - transform.position;
            var flat = new Vector3(dir.x, 0f, dir.z);

            if (flat.sqrMagnitude > 0.0001f)
            {
                var look = Quaternion.LookRotation(flat);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, rotateSpeed * Time.deltaTime);
            }

            float step = moveSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, step);

            if (Vector3.Distance(transform.position, targetPos) <= stopDistance) break;
            yield return null;
        }

        SetMoving(false);
        _isMoving = false;
        onDone?.Invoke();
    }

    public void LookAt(Transform target, bool isSnap = false)
    {
        if (target == null) return;
        Vector3 dir = target.position - transform.position; dir.y = 0f;
        if (dir.sqrMagnitude < 1e-4f) return;
        var rot = Quaternion.LookRotation(dir);
        transform.rotation = isSnap ? rot : Quaternion.Slerp(transform.rotation, rot, rotateSpeed * Time.deltaTime);
    }

    public IEnumerator SetHoldWeight(float to, float duration)
    {
        if (_animator == null || string.IsNullOrEmpty(holdWeightParam))
            yield break;

        float from = _animator.GetFloat(holdWeightParam);
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            _animator.SetFloat(holdWeightParam, Mathf.Lerp(from, to, t / duration));
            yield return null;
        }

        _animator.SetFloat(holdWeightParam, to);
    }

    private void SetMoving(bool on)     => _animator.SetBool(P_IsMoving, on);
    private void SetTwoHand(bool on)    => _animator.SetBool(P_Is2Hand, on);
    private void SetSadMoving(bool on)  => _animator.SetBool(P_IsSadMoving, on);

    private void Trigger(string name)
    {
        _animator.ResetTrigger(T_Wave);
        _animator.ResetTrigger(T_Inspect);
        _animator.ResetTrigger(T_Confuse);
        _animator.ResetTrigger(T_Saddle);
        _animator.SetTrigger(name);
    }
}
