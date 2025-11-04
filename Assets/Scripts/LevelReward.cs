using System;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Level Reward")]
public class LevelReward : ScriptableObject
{
    public int level;

    public RewardType type;

    // MODE
    public List<GAMEMODE> modes = new List<GAMEMODE>();

    // TOOL
    public List<STEP> tools = new List<STEP>();
    
    // UI
    public string displayName;

    // Helpers cho ShowIf
    public bool HasMode => (type & RewardType.Mode) != 0;
    public bool HasTool => (type & RewardType.Tool) != 0;
    public bool HasItem => (type & RewardType.Item) != 0;

    // (Optional) Hàm áp reward
    public void Apply()
    {
        if (HasMode && modes != null)
        {
            foreach (var m in modes)
                UserGameData.UnlockMode(m);
        }

        if (HasTool)
        {
            foreach (var t in tools)
                UserGameData.UnlockTool(t);
        }
           
    }
}

[Serializable]
public class StepRef
{
    public STEP step;
    public Sprite icon;
}