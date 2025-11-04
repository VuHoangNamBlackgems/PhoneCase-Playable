using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;


public static class UserGameData
{
    const string DATA_ID = "UserItemData";
    private static Data _data;

    #region Mode

    public static bool IsModeUnlocked(GAMEMODE mode)
        => GetData().modes.TryGetValue(mode, out var st) && st.isUnlock;

    public static void UnlockMode(GAMEMODE mode)
    {
        var d = GetData();
        if (!d.modes.TryGetValue(mode, out var st)) d.modes[mode] = st = new ModeData();
        if (!st.isUnlock)
        {
            st.isUnlock = true;
            Save();
        }
    }

    #endregion

    #region Tool

    public static bool IsToolUnlocked(STEP tool)
        => GetData().tools.TryGetValue(tool, out var st) && st.isUnlock;

    public static void UnlockTool(STEP tool)
    {
        var d = GetData();
        if (!d.tools.TryGetValue(tool, out var st)) d.tools[tool] = st = new ItemData();
        if (!st.isUnlock)
        {
            st.isUnlock = true;
            Save();
        }
    }

    #endregion

    #region Spray

    public static bool IsItemSprayUnlocked(int item)
        => GetData().itemsSpray.TryGetValue(item, out var st) && st.isUnlock;

    public static void UnlockItemSpray(int item)
    {
        var d = GetData();
        if (!d.itemsSpray.TryGetValue(item, out var st)) d.itemsSpray[item] = st = new ItemData();
        if (!st.isUnlock)
        {
            st.isUnlock = true;
            Save();
        }
    }

    #endregion
    
    #region Acrylic

    public static bool IsItemAcrylicUnlocked(int item)
        => GetData().itemsAcrylic.TryGetValue(item, out var st) && st.isUnlock;

    public static void UnlockItemAcrylic(int item)
    {
        var d = GetData();
        if (!d.itemsAcrylic.TryGetValue(item, out var st)) d.itemsAcrylic[item] = st = new ItemData();
        if (!st.isUnlock)
        {
            st.isUnlock = true;
            Save();
        }
    }

    #endregion
    
    #region Colorful

    public static bool IsItemColorfulUnlocked(int item)
        => GetData().itemsColorful.TryGetValue(item, out var st) && st.isUnlock;

    public static void UnlockItemColorful(int item)
    {
        var d = GetData();
        if (!d.itemsColorful.TryGetValue(item, out var st)) d.itemsColorful[item] = st = new ItemData();
        if (!st.isUnlock)
        {
            st.isUnlock = true;
            Save();
        }
    }

    #endregion
    
    #region Halloween

    public static bool IsItemHalloweenUnlocked(int item)
        => GetData().itemsHalloween.TryGetValue(item, out var st) && st.isUnlock;

    public static void UnlockItemHalloween(int item)
    {
        var d = GetData();
        if (!d.itemsHalloween.TryGetValue(item, out var st)) d.itemsHalloween[item] = st = new ItemData();
        if (!st.isUnlock)
        {
            st.isUnlock = true;
            Save();
        }
    }
    
    #endregion
    
    #region Glitter

    public static bool IsItemGlitterUnlocked(int item)
        => GetData().itemsGlitter.TryGetValue(item, out var st) && st.isUnlock;

    public static void UnlockItemGlitter(int item)
    {
        var d = GetData();
        if (!d.itemsGlitter.TryGetValue(item, out var st)) d.itemsGlitter[item] = st = new ItemData();
        if (!st.isUnlock)
        {
            st.isUnlock = true;
            Save();
        }
    }
    
    #endregion
    
    #region Sticker2D

    public static bool IsItemSticker2DUnlocked(int item)
        => GetData().itemsSticker2D.TryGetValue(item, out var st) && st.isUnlock;

    public static void UnlockItemSticker2D(int item)
    {
        var d = GetData();
        if (!d.itemsSticker2D.TryGetValue(item, out var st)) d.itemsSticker2D[item] = st = new ItemData();
        if (!st.isUnlock)
        {
            st.isUnlock = true;
            Save();
        }
    }
    
    #endregion
    
    #region Sticker3D

    public static bool IsItemSticker3DUnlocked(int item)
        => GetData().itemsSticker3D.TryGetValue(item, out var st) && st.isUnlock;

    public static void UnlockItemSticker3D(int item)
    {
        var d = GetData();
        if (!d.itemsSticker3D.TryGetValue(item, out var st)) d.itemsSticker3D[item] = st = new ItemData();
        if (!st.isUnlock)
        {
            st.isUnlock = true;
            Save();
        }
    }
    
    #endregion
    
    #region PhoneStrap

    public static bool IsItemPhoneStrapUnlocked(int item)
        => GetData().itemsPhoneStrap.TryGetValue(item, out var st) && st.isUnlock;

    public static void UnlockItemPhoneStrap(int item)
    {
        var d = GetData();
        if (!d.itemsPhoneStrap.TryGetValue(item, out var st)) d.itemsPhoneStrap[item] = st = new ItemData();
        if (!st.isUnlock)
        {
            st.isUnlock = true;
            Save();
        }
    }
    
    #endregion
    
    #region ChooseCase

    public static bool IsItemChooseCaseUnlocked(int item)
        => GetData().itemsChooseCase.TryGetValue(item, out var st) && st.isUnlock;

    public static void UnlockItemChooseCase(int item)
    {
        var d = GetData();
        if (!d.itemsChooseCase.TryGetValue(item, out var st)) d.itemsChooseCase[item] = st = new ItemData();
        if (!st.isUnlock)
        {
            st.isUnlock = true;
            Save();
        }
    }
    
    #endregion

    #region ToolSkin
    static string KeyUnlocked(ToolCategory cat, string id) => $"SHOP_{cat}_UNLOCK_{id}";
    static string KeySelected(ToolCategory cat) => $"SHOP_{cat}_SELECTED";

    public static bool IsUnlocked(ToolCategory cat, string id)
        => PlayerPrefs.GetInt(KeyUnlocked(cat, id), defaultValue: id.EndsWith("_01") ? 1 : 0) == 1; 
    // gợi ý: item _01 mặc định mở

    public static void SetUnlocked(ToolCategory cat, string id, bool value)
    {
        PlayerPrefs.SetInt(KeyUnlocked(cat, id), value ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static string GetSelected(ToolCategory cat)
        => PlayerPrefs.GetString(KeySelected(cat), "");

    public static void SetSelected(ToolCategory cat, string id)
    {
        PlayerPrefs.SetString(KeySelected(cat), id);
        PlayerPrefs.Save();
    }
    #endregion
    
    public static void Save()
    {
        PlayerPrefs.SetString(DATA_ID, JsonConvert.SerializeObject(GetData()));
    }
    
    public static Data GetData()
    {
        if (_data == null)
        {
            _data = JsonConvert.DeserializeObject<Data>(PlayerPrefs.GetString(DATA_ID));
            if (_data == null) _data = new Data();
        }

        return _data;
    }
    
    [System.Serializable]
    public class Data
    {
        public Dictionary<GAMEMODE, ModeData> modes = new Dictionary<GAMEMODE, ModeData>();
        public Dictionary<STEP, ItemData> tools = new Dictionary<STEP, ItemData>();
        public Dictionary<int, ItemData> itemsSpray = new Dictionary<int, ItemData>();
        public Dictionary<int, ItemData> itemsAcrylic = new Dictionary<int, ItemData>();
        public Dictionary<int, ItemData> itemsColorful = new Dictionary<int, ItemData>();
        public Dictionary<int, ItemData> itemsHalloween = new Dictionary<int, ItemData>();
        public Dictionary<int, ItemData> itemsGlitter = new Dictionary<int, ItemData>();
        public Dictionary<int, ItemData> itemsSticker2D = new Dictionary<int, ItemData>();
        public Dictionary<int, ItemData> itemsSticker3D = new Dictionary<int, ItemData>();
        public Dictionary<int, ItemData> itemsPhoneStrap = new Dictionary<int, ItemData>();
        public Dictionary<int, ItemData> itemsChooseCase = new Dictionary<int, ItemData>();
    }
    
    public class ItemData
    {
        public bool isUnlock = false;
    }
    
    [System.Serializable]
    public class ModeData
    {
        public bool isUnlock = false;
    }
}

