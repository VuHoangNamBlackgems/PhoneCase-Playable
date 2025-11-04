
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class HomeUI : MonoBehaviour
{
    [SerializeField] private Transform playBtn;
    [SerializeField] private Transform vipCustom;
    [SerializeField] private Transform btnDecor;
    [SerializeField] private Transform upgradeHint;
    [SerializeField] TMP_Text chatBoxText;
    [SerializeField] GameObject chatBox;
    [SerializeField] public RectTransform AdsPos;
    [SerializeField] private List<string> listChat = new List<string>();
    private void OnEnable()
    {
        playBtn.gameObject.SetActive(false);
        vipCustom.gameObject.SetActive(false);
        ShowChatBox();
        CheckVipCharacter();
        CheckUpgradeHint();
        //CallAdsManager.ShowONA("menu", AdsPos);
        btnDecor.gameObject.SetActive(true);
        
        
    }
    private void OnDisable()
    {
        //CallAdsManager.CloseONA("menu");
        HideChatBox();
        btnDecor.gameObject.SetActive(false);
    }

    public void CheckVipCharacter()
    {
        if (PlayerPrefs.GetInt("CharacterCount", 0) >= 3)
        {
            vipCustom.gameObject.SetActive(true);
            playBtn.gameObject.SetActive(false);
        }
        else
        {
            vipCustom.gameObject.SetActive(false);
            playBtn.gameObject.SetActive(true);
            playBtn.DOKill();
            playBtn.localScale = Vector3.zero;
            playBtn.DOScale(1f, 0.36f);
            
            DOVirtual.DelayedCall(0.37f, () =>
            {
                playBtn.localScale = Vector3.one;
                playBtn.DOKill();
                playBtn.DOScale(1.1f, 0.5f)
                    .SetEase(Ease.InQuad)
                    .SetLoops(-1, LoopType.Yoyo);
            });
            
        }
    }

    public void ShowChatBox()
    {
        chatBox.gameObject.SetActive(true);
        chatBoxText.text = listChat[UnityEngine.Random.Range(0, listChat.Count)];
        chatBoxText.gameObject.SetActive(true);
    }

    public void HideChatBox()
    {
        chatBox.gameObject.SetActive(false);
        chatBoxText.gameObject.SetActive(false);
    }

    public void CheckUpgradeHint()
    {
        bool canUpgrade = DecorManager.Instance.HasAnyDecorUpgradeable();
        Debug.Log(canUpgrade);
        if (canUpgrade)
        {
            upgradeHint.gameObject.SetActive(true);
        }
        else
        {
            upgradeHint.gameObject.SetActive(false);
        }
    }
}
