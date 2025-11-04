using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GlitterPreview : MonoBehaviour
{
    [SerializeField] Image icon;
    [SerializeField] GlitterDataSO glitterData;
    [SerializeField] private Button btn;
    [SerializeField] bool isUnlock = false;
    [SerializeField] Image imgReward;
    [SerializeField] Image imgSelect;
    public bool IsUnlock
    {
        get => isUnlock;
        set => isUnlock = value;
    }
    public GlitterDataSO GlitterData => glitterData;
    
    public void SetUp(GlitterDataSO glitterData, Action OnClick)
    {
        this.glitterData = glitterData;
        icon.sprite = glitterData.icon;
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
