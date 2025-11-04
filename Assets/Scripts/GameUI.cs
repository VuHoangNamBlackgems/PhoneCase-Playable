using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    
    [SerializeField] private List<Entry> entries = new List<Entry>();
    [SerializeField] GameObject rating;

    public static GameUI instance;
    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        //Show(UIScreen.Home);
    }

  
    public void Play_OnClick()
    {
        
    }

    public void Play_OnClickVip()
    {
        if (PlayerPrefs.GetInt("CharacterCount", 0) >= 3)
        {
          /*  CallAdsManager.ShowRewardVideo("vip_character", () =>
            {
                SingletonMono<Gameplay>.instance.IngameBG.gameObject.SetActive(true);
                Show(UIScreen.ChooseMode);
                UserTracking.CurrentCustomer = "vip_350";
            });*/
        }
    }

    public void ShowDecorUI()
    {
        Show(UIScreen.Decor);
      //  CameraController.instance.MoveCamera(CameraController.instance.MainCam.transform, CAMERA_POINT.DECORATION);
    }
    
    public void Skip()
    {
        Hide(UIScreen.Home);
   //     SingletonMono<Gameplay>.instance.CharacterManager.NextCharacter();
    }

    public void ShowRating()
    {
        rating.gameObject.SetActive(true);
    }
    
    public void HideRating()
    {
        rating.gameObject.SetActive(false);
    }

    public void CloseSettings()
    {
     //   CallAdsManager.ShowInter("close_settings");
        Hide(UIScreen.Settings);
        Show(UIScreen.Home);
    }
    
    public void OpenSettings()
    {
      //  CallAdsManager.ShowInter("open_settings");
        Show(UIScreen.Settings);
    }
    
    public void Show(UIScreen screen)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e.panel) e.panel.SetActive(e.screen == screen);
        }
    }
    
    public void Hide(UIScreen screen)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e.panel && e.screen == screen) e.panel.SetActive(false);
        }
    }
    
    public void HideAll()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            var p = entries[i].panel;
            if (p) p.SetActive(false);
        }
    }
}

[Serializable]
public struct Entry
{
    public UIScreen screen;
    public GameObject panel;
}

[Serializable]
public enum UIScreen
{
    Home = 0,
    ChooseMode = 1,
    EndGame = 2,
    Settings = 3,
    Rating = 4,
    Decor = 5
}