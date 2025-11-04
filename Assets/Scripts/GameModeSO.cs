using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameMode", menuName = "Phone Case/Game Mode")]
public class GameModeSO : ScriptableObject
{
    public GAMEMODE gameMode;

    public bool isUnlock = false;
    
    public List<STEP> listSteps;
    
    public List<PhoneCase> listPhoneCase;
    
    
}

[Serializable]
public enum GAMEMODE
{
    SPRAY = 0,
    ACRYLIC = 1,
    CHRISTMAS = 2,
    POPIT = 3,
    COLORFUL = 4,
    SPRAY_ART = 5,
    COMMINGSOON = 6,
    TUTORIAL = 7,
    FIX_PHONE_1 = 8,
    FIX_PHONE_2 = 9,
}