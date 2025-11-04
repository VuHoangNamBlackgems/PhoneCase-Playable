using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PhoneStrapPreview : MonoBehaviour
{
    [SerializeField] Image icon;
    [SerializeField] PhoneStrapDataSO phoneStrapData;
    [SerializeField] private Button btn;
    [SerializeField] bool isUnlock = false;
    [SerializeField] Image imgReward;
    [SerializeField] Image imgSelect;
    public bool IsUnlock
    {
        get => isUnlock;
        set => isUnlock = value;
    }
    
    public PhoneStrapDataSO PhoneStrapData => phoneStrapData;
    
    public void SetUp(PhoneStrapDataSO phoneStrapData, Action OnClick)
    {
        this.phoneStrapData = phoneStrapData;
        icon.sprite = phoneStrapData.icon;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(()=> OnClick());
    }
    
    public void SetUpUnlock(bool isUnlock)
    {
        IsUnlock = isUnlock;
        if(!isUnlock)
            imgReward.gameObject.SetActive(true);
        else
            imgReward.gameObject.SetActive(false);
    }

    public void Select(bool isSelect)
    {
        imgSelect.gameObject.SetActive(isSelect);
    }
}
