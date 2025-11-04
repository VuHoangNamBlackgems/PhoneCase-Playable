using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDataManager", menuName = "ItemDataManager")]
public class ItemDataManager : ScriptableObject
{

    private static ItemDataManager _instance;
    public static ItemDataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<ItemDataManager>("ItemDataManager");
#if UNITY_EDITOR
                if (_instance == null)
                {
                    Debug.LogError("ItemDataManager.asset không tìm thấy trong Resources folder!");
                }
#endif
            }

            return _instance;
        }
    }

    public List<PhoneCase> listPhoneCases = new List<PhoneCase>();
    public List<ItemDataSO> listSticker3D = new List<ItemDataSO>();
    public List<SprayDataSO> listSpray = new List<SprayDataSO>();
    public List<SprayDataSO> listAcrylic = new List<SprayDataSO>();
    public List<SprayDataSO> listColorful = new List<SprayDataSO>();
    public List<SprayDataSO> listHalloween = new List<SprayDataSO>();
    public List<GlitterDataSO> listGlitter = new List<GlitterDataSO>();
    public List<CharmDefinitionDataSO> listCharm = new List<CharmDefinitionDataSO>();
    public List<PhoneStrapDataSO> listPhoneStrap = new List<PhoneStrapDataSO>();
    public List<PopitCaseDataSO> listPopitCase = new List<PopitCaseDataSO>();
}
