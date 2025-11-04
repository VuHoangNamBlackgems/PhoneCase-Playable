using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class CustomerOrderReward : MonoBehaviour
{
     public enum Mode { Horizontal, Vertical, RectLoop, RandomHV }

    [Header("Targets (để trống = chính nó)")]
    [SerializeField] RectTransform ui; 
    [SerializeField] private Image toolOrderImage;     
    [SerializeField] Button rewardBtn;
    [SerializeField] CustomerOrderSO customerOrderSO;
    [Header("Config chậm & êm")]
    [SerializeField] Mode mode = Mode.RandomHV;
    [SerializeField] float distX = 18f;      
    [SerializeField] float distY = 14f;      
    [SerializeField] float legTime = 0.9f;   
    [SerializeField] bool playOnEnable = true;

    public CustomerOrderSO CustomerOrderSO => customerOrderSO;
    
    Sequence seq;
    Vector2 baseAP;
    Vector3 baseLP;
    public Action OnReward;

    public Button RewardBtn => rewardBtn;
    
    void Reset()
    {
        ui = GetComponent<RectTransform>();
    }

    private void Awake()
    {
        rewardBtn = GetComponent<Button>();
    }

    void OnEnable()
    {
        if (playOnEnable) Play();
       //rewardBtn.onClick.RemoveAllListeners();
       //rewardBtn.onClick.AddListener(Callback_Reward);
    }

    public void Callback_Reward()
    {
       /* CallAdsManager.ShowRewardVideo("rw_order", () =>
        {*/
            Hide();
            OnReward?.Invoke();
       // });
    }

    void OnDisable() => Stop();

    public void SetupTool(CustomerOrderSO customerOrderSO)
    {
        this.customerOrderSO = customerOrderSO;
        if (toolOrderImage) toolOrderImage.sprite = this.customerOrderSO.Sprite;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
    
    public void Play()
    {
        Stop();

        var m = mode == Mode.RandomHV
            ? (Random.value < 0.5f ? Mode.Horizontal : Mode.Vertical)
            : mode;

        if (ui) // UI
        {
            baseAP = ui.anchoredPosition;
            switch (m)
            {
                case Mode.Horizontal:
                    seq = DOTween.Sequence()
                        .Append(ui.DOAnchorPosX(baseAP.x + distX, legTime).SetEase(Ease.InOutSine))
                        .Append(ui.DOAnchorPosX(baseAP.x - distX, legTime).SetEase(Ease.InOutSine))
                        .SetLoops(-1, LoopType.Yoyo);
                    break;

                case Mode.Vertical:
                    seq = DOTween.Sequence()
                        .Append(ui.DOAnchorPosY(baseAP.y + distY, legTime).SetEase(Ease.InOutSine))
                        .Append(ui.DOAnchorPosY(baseAP.y - distY, legTime).SetEase(Ease.InOutSine))
                        .SetLoops(-1, LoopType.Yoyo);
                    break;

                case Mode.RectLoop:
                    seq = DOTween.Sequence()
                        .Append(ui.DOAnchorPos(new Vector2(baseAP.x + distX, baseAP.y),        legTime).SetEase(Ease.InOutSine))
                        .Append(ui.DOAnchorPos(new Vector2(baseAP.x + distX, baseAP.y + distY), legTime).SetEase(Ease.InOutSine))
                        .Append(ui.DOAnchorPos(new Vector2(baseAP.x - distX, baseAP.y + distY), legTime).SetEase(Ease.InOutSine))
                        .Append(ui.DOAnchorPos(new Vector2(baseAP.x - distX, baseAP.y),        legTime).SetEase(Ease.InOutSine))
                        .Append(ui.DOAnchorPos(baseAP,                                         legTime).SetEase(Ease.InOutSine))
                        .SetLoops(-1, LoopType.Restart);
                    break;
            }
        }
    }

    public void Stop()
    {
        if (seq != null && seq.IsActive()) seq.Kill();
        if (ui) ui.DOKill();
    }

    public void SetLegTime(float secondsPerLeg)
    {
        legTime = Mathf.Max(0.05f, secondsPerLeg);
        if (seq != null && seq.IsActive()) Play(); // restart với tốc độ mới
    }
}
