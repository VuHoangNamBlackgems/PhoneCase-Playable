using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

internal class PopitCasePreview : MonoBehaviour
{
    [SerializeField] Image icon;
    [SerializeField] PopitCaseDataSO popitCaseData;
    [SerializeField] private Button btn;
    [SerializeField] bool isUnlock = false;
    [SerializeField] Image imgReward;
    public bool IsUnlock
    {
        get => isUnlock;
        set => isUnlock = value;
    }
    public PopitCaseDataSO PopitCaseData => popitCaseData;

    private void Awake()
    {
        if(!icon) icon = GetComponent<Image>();
        if(!btn) btn = GetComponent<Button>();
    }

    public void SetUp(PopitCaseDataSO popitCaseData, Action OnClick)
    {
        this.popitCaseData = popitCaseData;
        icon.sprite = popitCaseData.icon;
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
    
}
