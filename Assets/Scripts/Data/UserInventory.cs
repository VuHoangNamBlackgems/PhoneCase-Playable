using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Player currency data
/// </summary>
public static class UserInventory
{
    /// <summary>
    /// Event call back change currency
    /// </summary>
    public static event Action<CurrencyType, int, int> CurrencyChangedHandler;

    /// <summary>
    /// add or remove currency value
    /// </summary>
    /// <param name="currencyType"></param>
    /// <param name="value"></param>
    /// <param name="raisedEvent"></param>
    /// <param name="save"></param>
    public static void ChangeCurrency(CurrencyType currencyType, int value, bool raisedEvent = true, bool save = false)
    {
        int currencyValue = GetCurrencyValue(currencyType);
        PlayerPrefs.SetInt(GetCurrencyId(currencyType), currencyValue + value);
        if(save) Save();
        if (raisedEvent) CurrencyChangedHandler?.Invoke(currencyType, currencyValue, value);
    }
    
    /// <summary>
    /// add or remove currency value
    /// </summary>
    /// <param name="currencyType"></param>
    /// <param name="value"></param>
    /// <param name="raisedEvent"></param>
    /// <param name="save"></param>
    public static void SetCurrency(CurrencyType currencyType, int value, bool raisedEvent = true, bool save = false)
    {
        int currencyValue = GetCurrencyValue(currencyType);
        PlayerPrefs.SetInt(GetCurrencyId(currencyType), value);
        if(save) Save();
        if(raisedEvent) CurrencyChangedHandler?.Invoke(currencyType, currencyValue, value);
    }

    /// <summary>
    /// return currency value
    /// </summary>
    /// <param name="currencyType"></param>
    /// <returns></returns>
    public static int GetCurrencyValue(CurrencyType currencyType)
    {
        return PlayerPrefs.GetInt(GetCurrencyId(currencyType));
    }
    
    public static void Save() { }
    
    private static string GetCurrencyId(CurrencyType currencyType)
    {
        switch (currencyType)
        {
            case CurrencyType.CASH:
                return "Player_TotalCash";
        }

        throw new Exception($"Can't find id currency type {currencyType.ToString()}");
    }
    
    

    public static CurrencyType ConvertToCurrencyType(string id)
    {
        foreach (CurrencyType currencyType in Enum.GetValues(typeof(CurrencyType)))
        {
            if (currencyType.ToString().Equals(id))
                return currencyType;
        }
        return default;
    }
}