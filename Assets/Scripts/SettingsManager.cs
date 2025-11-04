using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button soundBtn;
    [SerializeField] private Button musicBtn;
    [SerializeField] private Button vibraBtn;

    [Header("Icons (StateIcon)")]
    [SerializeField] private StateIcon _soundIconView;
    [SerializeField] private StateIcon _musicIconView;
    [SerializeField] private StateIcon _vibraIconView;

    void Awake()
    {
        if (soundBtn) soundBtn.onClick.AddListener(ToggleSfx);
        if (musicBtn) musicBtn.onClick.AddListener(ToggleMusic);
        if (vibraBtn) vibraBtn.onClick.AddListener(ToggleVibration);

        ApplyAllFromSaved();
    }

    void ApplyAllFromSaved()
    {
        if (_soundIconView) _soundIconView.Set(UserSettings.GetSoundVolume());
        if (_musicIconView) _musicIconView.Set(UserSettings.GetMusicVolume());
        if (_vibraIconView) _vibraIconView.Set(UserSettings.GetVibra());
    }

    public void ToggleSfx()
    {
        bool newState = !UserSettings.GetSoundVolume();
        UserSettings.SetSoundVolume(newState);
        if (_soundIconView) _soundIconView.Set(newState);
    }

    public void ToggleMusic()
    {
        bool newState = !UserSettings.GetMusicVolume();
        UserSettings.SetMusicVolume(newState);
        if (_musicIconView) _musicIconView.Set(newState);
    }

    public void ToggleVibration()
    {
        bool newState = !UserSettings.GetVibra();
        UserSettings.SetVibra(newState);
        if (_vibraIconView) _vibraIconView.Set(newState);

#if UNITY_ANDROID || UNITY_IOS
        if (newState) Handheld.Vibrate();
#endif
    }
}
