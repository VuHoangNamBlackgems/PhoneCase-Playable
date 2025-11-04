using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

public class Gameplay : MonoBehaviour
{
    [SerializeField] PhoneCase phoneCase;

    [SerializeField] Transform phonePos;
    
    [SerializeField] CharacterManager characterManager;
    
    [SerializeField] StepFlow stepFlow;
    
    [SerializeField] GameModeSO spray;

    [SerializeField] GameModeSO tutorial;
    
    [SerializeField] GameModeSO fixPhone1;
    
    public Transform IngameBG;

    public Transform verticalObj;
    
    public Transform horizontalObj;
    
    public Transform previewObj;
    
    public Transform screwObj;
    
    public PhoneCase currentPhoneCase;
    
    public ParticleSystem particleEndGame;
    
    [SerializeField] public RectTransform AdsPos;
    
    public CharacterManager CharacterManager => characterManager;

    private bool isMinigameFixPhone = false;
    
    public static Gameplay instance;
    private void Awake()
    {
        instance = this;
        Application.targetFrameRate = 60;
        CameraController.instance.MoveCamera(CameraController.instance.MainCam.transform, CAMERA_POINT.ONTABLE, false);
      //  CallAdsManager.instance.ShowBannerMenu();
        //CallAdsManager.ShowBanner();
    }

    private void Start()
    {

        /*if (PlayerPrefs.GetInt("Tutorial") == 0)
        {
            Tutorial();
        }
        else
        {
            GameUI.instance.Show(UIScreen.Home);
            characterManager.FirstSpawnCharacter();
        }*/
        // CallAdsManager.InitONA("before_game_play");
        // CallAdsManager.InitONA("end_level");
        PLayable();
    }

    public void PlayBtn()
    {
        //CallAdsManager.CloseONA("menu");
        IngameBG.gameObject.SetActive(true);
        UserTracking.CurrentCustomer = "normal_0";
        if (UserLevel.Level == 5 && PlayerPrefs.GetInt("FIX_PHONE_1", 0) == 0)
        {
            isMinigameFixPhone = true;
            GameUI.instance.HideAll();
            stepFlow.Steps = BuildStepsForMode(fixPhone1);
            UserTracking.CurrentMode = GAMEMODE.FIX_PHONE_1;

            CameraController.instance.MoveCamera(CameraController.instance.MainCam.transform, CAMERA_POINT.PREVIEW);
            SpawnPhoneCaseFix();
            phoneCase.SetUpVerticalPos(() =>
            {
                phoneCase.transform.DOLocalRotate(Vector3.up * 360f, 0.6f, RotateMode.LocalAxisAdd).SetEase(Ease.OutCubic);
                DOVirtual.DelayedCall(1.5f, () =>
                {
                    CameraController.instance.MoveCamera(CameraController.instance.MainCam.transform, CAMERA_POINT.ONTABLE);
                    phoneCase.SetupHorizonPos(() =>
                    {
                        UserTracking.LevelStart(stepFlow.Steps.Count);
                        stepFlow.JumpTo(0);
                    });
                });
            });
            //CallAdsManager.instance.ShowBannerMenu();
            return;
        }
        
        if (UserLevel.Level == 1)
        {
            GameUI.instance.HideAll();
            stepFlow.Steps = BuildStepsForMode(spray);
            UserTracking.CurrentMode = GAMEMODE.SPRAY;
            
            
            CameraController.instance.MoveCamera(CameraController.instance.MainCam.transform, CAMERA_POINT.PREVIEW);
            SpawnPhoneCase();
            phoneCase.SetUpVerticalPos(() =>
            {
                phoneCase.transform.DOLocalRotate(Vector3.up * 360f, 0.6f, RotateMode.LocalAxisAdd).SetEase(Ease.OutCubic);
                DOVirtual.DelayedCall(1.5f, () =>
                {
                    CameraController.instance.MoveCamera(CameraController.instance.MainCam.transform, CAMERA_POINT.ONTABLE);
                    phoneCase.SetupHorizonPos(() =>
                    {
                        UserTracking.LevelStart(stepFlow.Steps.Count);
                        stepFlow.JumpTo(0);
                    });
                });
            });
            //CallAdsManager.instance.ShowBannerMenu();
            return;
        }
        
      //  CallAdsManager.ShowInter("start_game");
        GameUI.instance.Show(UIScreen.ChooseMode);
        
    }
    
    public void SpawnPhoneCase()
    {
        var list = ItemDataManager.Instance.listPhoneCases;
        phoneCase = Instantiate(list[Random.Range(0, list.Count)], phonePos, true);
        phoneCase.SetupStartFlatPosition(horizontalObj, verticalObj, previewObj, screwObj);
        phoneCase.transform.position = verticalObj.position;
        phoneCase.gameObject.SetActive(true);
        stepFlow.PhoneCase = phoneCase;
    }

    public void SpawnPopitCase(PhoneCase popitCase)
    {
        phoneCase?.gameObject.SetActive(false);
        phoneCase = Instantiate(popitCase, phonePos, true);
        phoneCase.SetupStartFlatPosition(horizontalObj, verticalObj, previewObj, screwObj);
        stepFlow.PhoneCase = phoneCase;
        phoneCase.transform.position = horizontalObj.position;
        phoneCase.gameObject.SetActive(true);
        phoneCase.transform.rotation = Quaternion.Euler(90,0,0);
    }
    
    public void SpawnPhoneCaseFix()
    {
        phoneCase = Instantiate(fixPhone1.listPhoneCase[0], phonePos, true);
        phoneCase.SetupStartFlatPosition(horizontalObj, verticalObj, previewObj,  screwObj);
        phoneCase.transform.position = verticalObj.position;
        phoneCase.gameObject.SetActive(true);
        stepFlow.PhoneCase = phoneCase;
    }
    
    private List<StepBase> BuildStepsForMode(GameModeSO mode)
    {
        var ordered = new List<StepBase>();
        Debug.Log(mode.listSteps.Count);
        if (mode == null || mode.listSteps == null) return ordered;
        Debug.Log(mode.name);
        ProgressionStep.instance.IconsParent.gameObject.SetActive(true);
        ProgressionStep.instance.ClearIcons();

        foreach (var step in stepFlow.AllSteps)
        {
            step.GetStepUnLocked(fixPhone1);
        }
        
        foreach (var s in mode.listSteps)
        {
            var entry = stepFlow.AllSteps.Find(b => b.step == s && b.GetStepUnlocked());
            if (entry != null)
            {
                Debug.Log(s);
                ProgressionStep.instance.AddStep(entry.step);
                ordered.Add(entry);
            }
        }

        foreach (var st in ordered) if (st) st.gameObject.SetActive(false);
        return ordered;
    }
    
    public void ChooseMode(GameModeSO mode)
    {
        GameUI.instance.HideAll();
        stepFlow.Steps = BuildStepsForMode(mode);
        if (mode.gameMode == GAMEMODE.POPIT)
        {
            UserTracking.CurrentMode = GAMEMODE.POPIT;
            CameraController.instance.MoveCamera(CameraController.instance.MainCam.transform, CAMERA_POINT.ONTABLE, false);
            stepFlow.JumpTo(0);
            UserTracking.LevelStart(stepFlow.Steps.Count);
        }
        else
        {
            UserTracking.CurrentMode = mode.gameMode;
            CameraController.instance.MoveCamera(CameraController.instance.MainCam.transform, CAMERA_POINT.PREVIEW);
            SpawnPhoneCase();
            phoneCase.SetUpVerticalPos(() =>
            {
                phoneCase.transform.DOLocalRotate(Vector3.up * 360f, 0.6f, RotateMode.LocalAxisAdd).SetEase(Ease.OutCubic);
                DOVirtual.DelayedCall(1.5f, () =>
                {
                    CameraController.instance.MoveCamera(CameraController.instance.MainCam.transform, CAMERA_POINT.ONTABLE);
                    phoneCase.SetupHorizonPos(() =>
                    {
                        UserTracking.LevelStart(stepFlow.Steps.Count);
                        stepFlow.JumpTo(0);
                    });
                });
            });
        }

        StartCoroutine(ShowAdsBanner());
    }

    IEnumerator ShowAdsBanner()
    {
      //  CallAdsManager.ShowONA("before_game_play");
        yield return new WaitForSeconds(4f);
      //  CallAdsManager.instance.ShowBannerMenu();
    }

    public void Preview()
    {
        phoneCase.SetupPreviewPos(null);
        CameraController.instance.MoveCamera(CameraController.instance.MainCam.transform, CAMERA_POINT.PREVIEW);
    }

    public void EndGame()
    {
        CameraController.instance.MoveCamera(CameraController.instance.MainCam.transform, CAMERA_POINT.PREVIEW);
        GameUI.instance.HideAll();
        phoneCase.SetupPreviewPos(()=>
        {
            particleEndGame.Play();
            AudioManager.Instance.PlayComplete();
            phoneCase.transform.DOLocalRotate(Vector3.up * 360f, 0.6f, RotateMode.LocalAxisAdd).SetEase(Ease.OutCubic);
            Debug.Log("end 1");
            DOTween.Sequence()
                .AppendInterval(1.5f)
                .AppendCallback(() =>
                {
                    Debug.Log("end 2");
                    CameraController.instance.CapCam.gameObject.SetActive(true);
                    GameUI.instance.Show(UIScreen.EndGame);
                    if(isMinigameFixPhone) PlayerPrefs.SetInt("FIX_PHONE_1", 1);
                    if (PlayerPrefs.GetInt("Tutorial") != 0)
                    {
                        UserTracking.LevelEndInternal("Win");
                    }
                    else
                    {
                        UserTracking.TutorialAction(ActionTut.finish);
                    }
                  //  CallAdsManager.ShowInter("finish_step");
                });
            Debug.Log("end 3");
        });
      //  CallAdsManager.instance.ShowBannerMenu();
    }

    public void Complete()
    {
        GameUI.instance.Hide(UIScreen.EndGame);
        ProgressionStep.instance.ClearIcons();
        IngameBG.gameObject.SetActive(false);
        phoneCase?.gameObject.SetActive(false);
        CameraController.instance.MoveCamera(CameraController.instance.MainCam.transform, CAMERA_POINT.DEFAULT, false);
        characterManager.NextCharacter();
    }

    public void Tutorial()
    {
        UserTracking.TutorialAction(ActionTut.start);
        IngameBG.gameObject.SetActive(true);
        CameraController.instance.MoveCamera(CameraController.instance.MainCam.transform, CAMERA_POINT.ONTABLE, false);

        GameUI.instance.HideAll();
        stepFlow.Steps = BuildStepsForMode(tutorial);
        
        var list = tutorial.listPhoneCase;
        phoneCase = Instantiate(list[0], phonePos, true);
        stepFlow.PhoneCase = phoneCase;
        phoneCase.transform.position = horizontalObj.position;
        phoneCase.transform.rotation = Quaternion.Euler(90,0,0);
        phoneCase.gameObject.SetActive(true);
        phoneCase.SetupStartFlatPosition(horizontalObj, verticalObj, previewObj, screwObj);
        DOVirtual.DelayedCall(0.5f, () =>
        {
            stepFlow.JumpTo(0);
        });

    }

    public void PLayable()
    {
        IngameBG.gameObject.SetActive(true);
        CameraController.instance.MoveCamera(CameraController.instance.MainCam.transform, CAMERA_POINT.ONTABLE, false);

        GameUI.instance.HideAll();
        //stepFlow.Steps = BuildStepsForMode(tutorial);

        var list = ItemDataManager.Instance.listPhoneCases;
        phoneCase = Instantiate(list[2], phonePos, true);
        stepFlow.PhoneCase = phoneCase;
        phoneCase.transform.position = horizontalObj.position;
        phoneCase.transform.rotation = Quaternion.Euler(90, 0, 0);
        phoneCase.gameObject.SetActive(true);
        phoneCase.SetupStartFlatPosition(horizontalObj, verticalObj, previewObj, screwObj);
        DOVirtual.DelayedCall(0.5f, () =>
        {
            stepFlow.JumpTo(0);
        });

    }

    public void OnApplicationPause(bool pauseStatus)
    {
        if(Application.platform != RuntimePlatform.Android) return;
        if (pauseStatus && GameState.State == State.RUNNING && PlayerPrefs.GetInt("Tutorial") != 0)
        {
            UserTracking.LevelExit();
        }
    }
}
