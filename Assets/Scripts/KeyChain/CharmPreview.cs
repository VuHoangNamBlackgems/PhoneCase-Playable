using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CharmPreview : MonoBehaviour
{
    public Image icon;
    public CharmDefinitionDataSO definition;
    public Button btn;
    [SerializeField] bool isUnlock = false;
    [SerializeField] Image imgReward;
    [SerializeField] Image imgSelect;
    public bool IsUnlock
    {
        get => isUnlock;
        set => isUnlock = value;
    }
    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            if (definition) CharmEvents.RequestSpawn(definition, this);
        });
    }
    public void SetUp(CharmDefinitionDataSO definition)
    {
        this.definition = definition;
        icon.sprite = definition.icon;
    }
    
    public void SetUpUnlock(bool isUnlock)
    {
        IsUnlock = isUnlock;
        if(!isUnlock)
            imgReward.gameObject.SetActive(true);
        else
            imgReward.gameObject.SetActive(false);
    }

    public void Select(bool isSelect)
    {
        imgSelect.gameObject.SetActive(isSelect);
    }
}