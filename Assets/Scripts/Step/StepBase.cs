using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class StepBase : MonoBehaviour, IStep
{
    public Sprite Icon { get; }

    public STEP step;
    
    [SerializeField]
    protected Transform _btnDone, _btnRevert;

    public abstract void SetUp(PhoneCase phoneCase);

    public abstract void CompleteStep();

  
    public bool GetStepUnlocked()
    {
        if (step == STEP.Blur || step == STEP.Spray || step == STEP.Glue)
        {
            return true;
        }
        else
        {
            Debug.Log(step);
            return UserGameData.IsToolUnlocked(step);
        }
    }

    public bool GetStepUnLocked(GameModeSO mode)
    {
        if (mode.gameMode == GAMEMODE.FIX_PHONE_1)
        {
            foreach (var step in mode.listSteps)
            {
                UserGameData.UnlockTool(step);
                Debug.Log(step);
            }
            return true;
        }
        return false;
    }
}

