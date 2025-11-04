using System;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum RewardType
{
    None = 0,
    Mode = 1 << 0,
    Tool = 1 << 1,
    Item = 1 << 2,
}

public class LevelRewardsManager : MonoBehaviour
{
    [Header("Config")] [Tooltip("All rewards definitions. Multiple entries can share the same level.")]
    public List<LevelReward> rewards = new List<LevelReward>();


    [Header("Popup UI")] [Tooltip("UI popup that showcases the newly unlocked feature(s)")]
    public NewFeatureUI popup;


    private readonly Queue<LevelReward> _queue = new Queue<LevelReward>();
    private bool _showing;
    public bool AllRewardsDone => !_showing && _queue.Count == 0;

    public static LevelRewardsManager instance { get; private set; }


    private void Awake()
    {
        instance = this;
    }
    public void OnLevelReached(int level)
    {
        for (int i = 0; i < rewards.Count; i++)
        {
            var r = rewards[i];
            if (r != null && r.level == level)
            {
                _queue.Enqueue(r);
                ApplyReward(r);
            }
        }

        TryShowNext();
    }

    public void UnlockLevel(int level)
    {
        for (int i = 0; i < rewards.Count; i++)
        {
            var r = rewards[i];
            if (r != null && r.level == level)
            {
                _queue.Enqueue(r);
                ApplyReward(r);
            }
        }
    }


    public void OnPopupClosed()
    {
        _showing = false;
        if (AllRewardsDone)
        {
            if (PlayerPrefs.GetInt("RATE") == 0)
            {
                GameUI.instance.ShowRating();
            }
        }
        TryShowNext();
    }


    private void TryShowNext()
    {
        if (_showing || _queue.Count == 0)
            return;

        _showing = true;
        var r = _queue.Dequeue();
        //ApplyReward(r);
        SetupPopup(r);
    }

    private void SetupPopup(LevelReward r)
    {
        if (popup == null)
        {
            _showing = false;
            TryShowNext();
            return;
        }


        if (!string.IsNullOrEmpty(r.displayName))
            popup.SetName(r.displayName);

        bool visualSet = false;
        if (r.HasTool && r.tools != null && r.tools.Count > 0)
        {
            popup.gameObject.SetActive(true);
            popup.SetUpUnlockItem(r.tools[0]);
            visualSet = true;
        }

        popup.gameObject.SetActive(true);
        popup.PlayIntro();
    }
    
    private void ApplyReward(LevelReward r)
    {
        if (r == null) return;

        if (r.HasMode && r.modes != null)
        {
            for (int i = 0; i < r.modes.Count; i++)
            {
                UserGameData.UnlockMode(r.modes[i]);
            }
        }

        if (r.HasTool && r.tools != null)
        {
            for (int i = 0; i < r.tools.Count; i++)
            {
                UserGameData.UnlockTool(r.tools[i]);
            }
        }
    }
}