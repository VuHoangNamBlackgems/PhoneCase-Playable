using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProgressionStep : MonoBehaviour
{
    [SerializeField] List<ProgressionStepData> steps = new List<ProgressionStepData>(); 
    [SerializeField] IconProgression iconProgressionPrefab;   
    [SerializeField] Transform iconsParent;    
    
    public Transform IconsParent => iconsParent;

    readonly List<IconProgression> _spawned = new List<IconProgression>();          
    int _index;
    public static ProgressionStep instance { get; private set; }
    private void Awake()
    {
        instance = this;
    }

    void OnDisable() => ClearIcons();

    public void Build(List<ProgressionStepData> newSteps)
    {
        steps = newSteps ?? new List<ProgressionStepData>();
        SetIndex(0, instant: true);
    }

    public void SpawnStep(GameModeSO gameModeSO)
    {
        ClearIcons();
        if (steps == null || steps.Count == 0 || iconProgressionPrefab == null || iconsParent == null) return;
        var stepsMode = gameModeSO.listSteps;
        for (int i = 0; i < stepsMode.Count; i++)
        {
            if (UserGameData.IsToolUnlocked(stepsMode[i]))
            {
                var icon = Instantiate(iconProgressionPrefab, iconsParent);
                icon.Step = stepsMode[i];
                Debug.Log(icon.Step);
                var stepData = steps.Find(step => step.Step == stepsMode[i]);
                icon.SetIcon(stepData.sprStep);
                _spawned.Add(icon);
            }
        }
    }
    
    public void AddStep(STEP step)
    {
          var icon = Instantiate(iconProgressionPrefab, iconsParent);
          icon.Step = step;
          var a = steps.Find(x => x.Step == step);
          icon.SetIcon(a.sprStep);
          _spawned.Add(icon);
    }

    public void ClearIcons()
    {
        for (int i = 0; i < _spawned.Count; i++)
            if (_spawned[i]) Destroy(_spawned[i].gameObject);
        _spawned.Clear();
        _index = 0;
    }
    
    public void SetIndex(int index, bool instant = false)
    {
        if (_spawned.Count == 0) return;
        _index = Mathf.Clamp(index, 0, _spawned.Count - 1);
        for (int i = 0; i < _spawned.Count; i++)
        {
            bool active = i == _index;
            if (active)
                _spawned[i].Selected();
            else
                _spawned[i].UnSelected();
        }
        _spawned[_index].IsDone = true;
    }

    public void Next()
    {
        if (_spawned.Count == 0) return;
        
        if (_index + 1 < _spawned.Count) SetIndex(_index + 1);
        else
        {
            ClearIcons();
        }
    }
}

[Serializable]
public class ProgressionStepData
{
    public STEP Step;
    public Sprite sprStep;
}
