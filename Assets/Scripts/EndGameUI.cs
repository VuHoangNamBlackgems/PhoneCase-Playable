using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndGameUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private Image imageCase;
    [SerializeField] private TMP_Text txtMoney;
    [SerializeField] private bool autoCaptureOnEnable = true;
    [SerializeField] private bool preserveAspect = true;
    [SerializeField] private float spritePixelsPerUnit = 100f;
    // [SerializeField] private RectTransform AdsPos;
    
    private Sprite _runtimeSprite;
    bool _progressGranted;
    private void OnEnable()
    {
        if (CameraController.instance)
            CameraController.instance.onImageReady.AddListener(OnImageReady);

        if (autoCaptureOnEnable)
            CameraController.instance?.CaptureImage();
        if (PlayerPrefs.GetInt("CharacterCount", 0) >= 3)
        {
            txtMoney.text = "+300";
        }
        else
        {
            txtMoney.text = "+50";
        }
        
        _progressGranted = false;
      //  CallAdsManager.HideBanner();
       // CallAdsManager.ShowONA("end_level");
    }

    private void OnDisable()
    {
       // CallAdsManager.instance.ShowBannerMenu();
       // CallAdsManager.CloseONA("end_level");
        if (CameraController.instance)
            CameraController.instance.onImageReady.RemoveListener(OnImageReady);
    }

    private void OnDestroy()
    {
        if (_runtimeSprite) Destroy(_runtimeSprite);
    }

    private void OnImageReady(Texture2D tex)
    {
        if (!imageCase || !tex) return;

        if (_runtimeSprite) Destroy(_runtimeSprite);
        _progressGranted = false;
        _runtimeSprite = Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            spritePixelsPerUnit
        );
        Debug.Log(_runtimeSprite);
        imageCase.sprite = _runtimeSprite;
        imageCase.preserveAspect = preserveAspect;
    }

    public void CaptureNow() => CameraController.instance?.CaptureImage();

    public void OnClick_Share()
    {
        UserInventory.ChangeCurrency(CurrencyType.CASH, 50);
        PlayerPrefs.SetInt("Tutorial", 1);
        CheckLevelUp();
    }
    
    public void OnClick_ClaimX2()
    {
        Debug.Log($"Reward Ad Show");
        if (PlayerPrefs.GetInt("CharacterCount", 0) >= 3)
        {
            UserInventory.ChangeCurrency(CurrencyType.CASH, 600);
        }
        else
        {
            UserInventory.ChangeCurrency(CurrencyType.CASH, 100);
        }
        UserTracking.TutorialAction(ActionTut.claim_X2);
        PlayerPrefs.SetInt("Tutorial", 1);
        CheckLevelUp();
    }

    public void OnClick_NoThanks()
    {
       // CallAdsManager.ShowInter("btn_no_thanks");
        if (PlayerPrefs.GetInt("CharacterCount", 0) >= 3)
        {
            UserInventory.ChangeCurrency(CurrencyType.CASH, 300);
        }
        else
        {
            UserInventory.ChangeCurrency(CurrencyType.CASH, 50);
        }
        UserTracking.TutorialAction(ActionTut.no_thanks);
        PlayerPrefs.SetInt("Tutorial", 1);
        CheckLevelUp();
    }

    void CheckLevelUp()
    {
        Gameplay.instance.Complete();
        
        int prevLv = UserLevel.Level;
        int ups = UserLevel.AddPlay();      
        if (ups <= 0) return;

        for (int i = 1; i <= ups; i++)
        {
            int reachedLv = prevLv + i;
            LevelRewardsManager.instance.OnLevelReached(reachedLv);
        }

        Hide();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}