using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Item Data", menuName = "Phone Case/ItemDataSO")]
public class ItemDataSO : ScriptableObject
{
    public int id;
    
    public bool isUnlock = false;
    
    public Sprite _iconPreview;
    
    public BuyProperties BuyProperties;
}

[System.Serializable]
public class BuyProperties
{
    public CurrencyType CurrencyType;
    public int Amount;
}

[System.Serializable]
public enum CurrencyType
{
    CASH , WATCHAD
}