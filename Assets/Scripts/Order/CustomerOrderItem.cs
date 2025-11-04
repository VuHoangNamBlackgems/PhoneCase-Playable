using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Order
{
    public class CustomerOrderItem : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private RectTransform root;       
        [SerializeField] private Image itemOrderImage;     
        [SerializeField] private GameObject checkIcon;
    
        [Header("Effects")]
        [SerializeField] private ParticleSystem checkVfx;

        [Header("Data")]
        [SerializeField] private int requiredId;
        public int RequiredId => requiredId;

        [Header("Anim")]
        [SerializeField] private Vector2 hideOffset = new Vector2(220f, 0f);
        [SerializeField] private float showDuration = 0.25f;
        [SerializeField] private float hideDuration = 0.18f;
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField] private Ease hideEase = Ease.InBack;
        [SerializeField] private bool startHidden = false;
        
        [Header("Idle Loop (scale + wobble)")]
        [SerializeField] private bool loopIdle = true;
        [SerializeField] private float pulseScale = 1.06f;       
        [SerializeField] private float pulseDuration = 0.6f;     
        [SerializeField] private float wobbleAngle = 7f;         
        [SerializeField] private float wobbleDuration = 0.5f;    
        [SerializeField] private float loopDesyncMax = 0.2f;  

        CanvasGroup cg;
        Sequence seq;
        Sequence seqloop;
        Vector2 shownPos, hiddenPos;

        void Awake()
        {
            if (!root) root = (RectTransform)transform;
            cg = GetComponent<CanvasGroup>();
            if (!cg) cg = gameObject.AddComponent<CanvasGroup>();

            hiddenPos = shownPos + hideOffset;
        }

        void OnEnable()
        {
            KillSeq();
            if (startHidden) SetHiddenInstant();
            if (checkIcon) checkIcon.SetActive(false);
        }


        public void SetupOrder(int requiredId, Sprite sprite)
        {
            this.requiredId = requiredId;
            if (itemOrderImage) itemOrderImage.sprite = sprite;
            if (checkIcon) checkIcon.SetActive(false);
        }
        public void SetUpOrder(int requiredId, Sprite sprite) => SetupOrder(requiredId, sprite);

        public void ShowOrder(float delay = 0f)
        {
            gameObject.SetActive(true);
            KillSeq();
            cg.alpha = 0f;

            seq = DOTween.Sequence()
                .AppendInterval(delay)
                .Join(cg.DOFade(1f, showDuration * 0.9f));
            StartIdleLoop();
        }

        public void HideOrder(float delay = 0f, Action onHidden = null)
        {
            KillSeq();
            seq = DOTween.Sequence()
                .AppendInterval(delay)
                .Join(cg.DOFade(0f, hideDuration * 0.9f))
                .OnComplete(() =>
                {
                    onHidden?.Invoke();
                });
        }

        public void ShowCheck()
        {
            if (!checkIcon) return;
            checkIcon.SetActive(true);

            var t = checkIcon.transform;
            t.DOKill(true);
            t.localScale = Vector3.one * 0.6f;
            t.DOScale(1.05f, 0.18f).SetEase(Ease.OutBack, 1.6f).OnComplete(() =>
                t.DOScale(1f, 0.12f));

            if (checkVfx) checkVfx.Play();
            StopIdleLoop();
        }

        public void HideCheck()
        {
            if (!checkIcon) return;
            var t = checkIcon.transform;
            t.DOKill(true);
            t.DOScale(0.6f, 0.12f).OnComplete(() =>
            {
                checkIcon.SetActive(false);
                t.localScale = Vector3.one;
            });
        }
        
        void StartIdleLoop()
        {
            seqloop?.Kill();
                var t = itemOrderImage.transform;
                t.localScale = Vector3.one;
            
                seqloop = DOTween.Sequence()
                    .Append(t.DOScale(1.1f, 0.3f))
                   // .Append(DOTweenManager.Ins.ShakeFail(t))
                    .AppendInterval(1f) 
                    .SetLoops(-1, LoopType.Restart);
        }

        void StopIdleLoop()
        {
            seqloop.Kill();
            itemOrderImage.transform.localScale = Vector3.one;
            itemOrderImage.transform.localRotation = Quaternion.identity;
        }


        public void Complete()
        {
            ShowCheck();
        }

        void SetHiddenInstant()
        {
            cg.alpha = 0f;
        }

        void KillSeq()
        {
            if (seq != null && seq.IsActive()) seq.Kill();
            root.DOKill();
            cg.DOKill();
        }
    
    }
}
