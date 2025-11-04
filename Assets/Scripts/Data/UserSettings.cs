using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UserSettings 
{
    [System.Serializable]
    public class Data
    {
        public bool SoundVolume;
        public bool MusicVolume;
        public bool Vibra;
    }
    const string DATA_ID = "UserSetting";
    private static Data _data;

    public static bool GetSoundVolume()
    {
        Data data = GetData();
        return data.SoundVolume;
    }
    
    public static bool GetMusicVolume()
    {
        Data data = GetData();
        return data.MusicVolume;
    }

    public static bool GetVibra()
    {
        Data data = GetData();
        return data.Vibra;
    }
    
    public static void SetSoundVolume(bool value)
    {
        Data _data = GetData();
        _data.SoundVolume = value;
    }
    
    public static void SetMusicVolume(bool value)
    {
        Data _data = GetData();
        _data.MusicVolume = value;
    }
    
    public static void SetVibra(bool value)
    {
        Data _data = GetData();
        _data.Vibra = value;
    }
        
        
    public static void Save()
    {
        PlayerPrefs.SetString(DATA_ID, JsonUtility.ToJson(GetData()));
    }
    
    public static Data GetData()
    {
        if (_data == null)
        {
            _data = JsonUtility.FromJson<Data>(PlayerPrefs.GetString(DATA_ID));
            if(_data == null) _data = new Data();
        }
        return _data;
    }
}
