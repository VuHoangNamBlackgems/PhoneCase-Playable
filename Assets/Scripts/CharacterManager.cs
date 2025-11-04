using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class CharacterManager : MonoBehaviour
{
    [SerializeField] private Transform[] _characterPos;
    
    [SerializeField] private Transform[] _moveOutPath;
    
    [SerializeField] private Transform[] _moveInPath;
    
    [SerializeField] private Transform[] _doorPos;
    
    [SerializeField] private ModelController[] _models;
    
    [SerializeField] private ModelController[] _modelsVip;
    
    [SerializeField] private ModelController _currentModel;
    
    public ModelController CurrentModel => _currentModel;

    public Transform lookAt;

    private int count = 0;
    private void Start()
    {
       
        if (!PlayerPrefs.HasKey("CharacterCount"))
            PlayerPrefs.SetInt("CharacterCount", count);
        
    }

    public void FirstSpawnCharacter()
    {
        if (PlayerPrefs.GetInt("CharacterCount", 0) >= 3)
        {
            var characterVip = _modelsVip[Random.Range(0, _modelsVip.Length)];
            _currentModel = Instantiate(characterVip, _characterPos[Random.Range(0, _characterPos.Length)].position, Quaternion.identity);
            _currentModel.transform.position = _characterPos[0].position;
            _currentModel.transform.rotation = Quaternion.Euler(new Vector3(0,180,0));
            return;
        }
        var character = _models[Random.Range(0, _models.Length)];
        _currentModel = Instantiate(character, _characterPos[Random.Range(0, _characterPos.Length)].position, Quaternion.identity);
        _currentModel.transform.position = _characterPos[0].position;
        _currentModel.transform.rotation = Quaternion.Euler(new Vector3(0,180,0));
    }
    
    public void SpawnCharacter()
    {
        if (PlayerPrefs.GetInt("CharacterCount", 0) >= 3)
        {
            Debug.Log("Spawn_Vip");
            var characterVip = _modelsVip[Random.Range(0, _modelsVip.Length)];
            _currentModel = Instantiate(characterVip, _characterPos[Random.Range(0, _characterPos.Length)].position, Quaternion.identity);
            _currentModel.transform.position = _moveInPath[0].position;
            OpenDoor();
            MoveIn();
            return;
        }
        var character = _models[Random.Range(0, _models.Length)];
        _currentModel = Instantiate(character, _characterPos[Random.Range(0, _characterPos.Length)].position, Quaternion.identity);
        _currentModel.transform.position = _moveInPath[0].position;
        OpenDoor();
        MoveIn();
    }
    
    public void NextCharacter()
    {
        if (PlayerPrefs.GetInt("CharacterCount", 0) >= 3)
        {
            PlayerPrefs.SetInt("CharacterCount", 0);
            PlayerPrefs.Save();
        }
        
        var tmp = PlayerPrefs.GetInt("CharacterCount", 0) + 1;
        PlayerPrefs.SetInt("CharacterCount", tmp);
        PlayerPrefs.Save();
        Debug.Log(PlayerPrefs.GetInt("CharacterCount"));
        MoveOut();
        DOVirtual.DelayedCall(0.2f, () =>
        {
            SpawnCharacter();
        });
    }
    
    public void OpenDoor()
    {
        AudioManager.Instance.PlayDoorOpen();
        _doorPos[0].DOLocalRotateQuaternion(Quaternion.Euler(-90,90,0), 1f).SetEase(Ease.InOutSine);
        _doorPos[1].DOLocalRotateQuaternion(Quaternion.Euler(-90,-90,0), 1f).SetEase(Ease.InOutSine);
    }

    public void CloseDoor()
    {
        _doorPos[0].DOLocalRotateQuaternion(Quaternion.Euler(-90,0,0), 1f).SetEase(Ease.InOutSine);
        _doorPos[1].DOLocalRotateQuaternion(Quaternion.Euler(-90,0,0), 1f).SetEase(Ease.InOutSine);
    }

    public void MoveIn()
    {
        _currentModel.FollowPath(_moveInPath, () =>
        {
            CloseDoor();
            _currentModel.transform.rotation = Quaternion.Euler(new Vector3(0,180,0));
            GameUI.instance.Show(UIScreen.Home);
        });
    }
    
    public void MoveOut()
    {
        _currentModel?.MoveOut(_moveOutPath);
        _currentModel = null;   
    }
}
