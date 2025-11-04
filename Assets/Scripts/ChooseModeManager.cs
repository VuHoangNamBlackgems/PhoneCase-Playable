using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ChooseModeManager : MonoBehaviour
{
    [SerializeField] private List<GameMode> listMode;

    private void OnEnable()
    {
        foreach (GameMode mode in listMode)
        {
            mode.transform.localScale = Vector3.zero;
            mode.SetUpUnlock();
        }
        SetUpMode();
    }

    public void SetUpMode()
    {
        float delayTime = 0;
        for (int i = 0; i < listMode.Count; i++)
        {
            listMode[i].transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetDelay(delayTime);
            delayTime += 0.1f;
        }
    }
    
    public void OnClickHide(bool isFade = true)
    {
        
    }

    public void OnClick_Inter()
    {
     ///   CallAdsManager.ShowInter("btn_mode");
    }
}
