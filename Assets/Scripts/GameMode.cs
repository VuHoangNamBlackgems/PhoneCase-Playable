using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

public class GameMode : MonoBehaviour
{
    [SerializeField] GameModeSO gameModeSO;
    [SerializeField] Button btn;
    [SerializeField] Transform lockContent;
    [SerializeField] Image gameModeImage;
    bool loadingGameMode = false;

    private void Awake()
    {
       // loadingGameMode = GameConfig.instance.InGameConfig.loadingGameMode;
    }

    private void Start()
    {
         btn.onClick.AddListener(OnClick_GameMode);
    }
    

    public void OnClick_GameMode()
    {
        bool unlock = UserGameData.IsModeUnlocked(gameModeSO.gameMode);
        if (unlock)
        {
            if (loadingGameMode)
            {

                SimpleSplashAnim.instance.Play();
                SimpleSplashAnim.instance.onCompleted = null;
                SimpleSplashAnim.instance.onCompleted += () =>
                {
                    Gameplay.instance.ChooseMode(gameModeSO);
                    UserTracking.CurrentMode = gameModeSO.gameMode;
                };
               // CallAdsManager.ShowInter("start_level");
                return;
            }
            Gameplay.instance.ChooseMode(gameModeSO);
            UserTracking.CurrentMode = gameModeSO.gameMode;
            //CallAdsManager.ShowInter("start_level");
        }
    }

    public void SetUpUnlock()
    {
        if(gameModeSO.gameMode == GAMEMODE.COMMINGSOON) return;
        if(gameModeSO.gameMode == GAMEMODE.SPRAY)
            UserGameData.UnlockMode(gameModeSO.gameMode);

        bool unlock = UserGameData.IsModeUnlocked(gameModeSO.gameMode);
        Debug.Log(gameModeSO.name + unlock);
        lockContent.gameObject.SetActive(!unlock);
        if(unlock)
            gameModeImage.color = Color.white;
        else
            gameModeImage.color = new Color32(53, 53, 53, 255);
    }
}
