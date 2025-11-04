using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SprayPreview : MonoBehaviour
{
    [SerializeField] Image icon;
    [SerializeField] SprayDataSO sprayData;
    [SerializeField] Button btn;
    [SerializeField] bool isUnlock = false;
    [SerializeField] Image imgReward;
    [SerializeField] Image imgSelect;
    public bool IsUnlock
    {
        get => isUnlock;
        set => isUnlock = value;
    }

    public SprayDataSO SprayData => sprayData;
    
    public void SetUp(SprayDataSO sprayData, Action OnClick)
    {
        this.sprayData = sprayData;
        icon.sprite = sprayData.icon;
        icon.SetNativeSize();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(()=> OnClick());
    } 
    
    public void Select()
    {
        imgSelect.gameObject.SetActive(true);
    }

    public void Unselect()
    {
        imgSelect.gameObject.SetActive(false);
    }

    public void SetUpUnlock(bool isUnlock)
    {
        IsUnlock = isUnlock;
        if(!isUnlock)
            imgReward.gameObject.SetActive(true);
        else
            imgReward.gameObject.SetActive(false);
    }
    
}
