using System.Collections.Generic;

using UnityEngine;

public static class UserTracking
{
    // ===== PlayerPrefs keys =====
    private const string KEY_TOTAL_PLAY = "USER_TOTAL_PLAY_COUNT";
    private static string KeyAttempt(string levelName) => $"LV_ATTEMPT_{levelName}";

    // ===== Public state =====
    public static GAMEMODE CurrentMode { get; set; }
    public static string CurrentCustomer { get; set; }

    // (giữ nguyên) tracking theo "mode"
    static int stepsPlayedInSession;
    static bool modeOpen;

    // ===== Level session (level_start / level_end) =====
    struct LevelCtx
    {
        public bool active;
        public string levelName;
        public string customer;          
        public GAMEMODE mode;
        public int totalSteps;
        public int clearedSteps;
        public int playIndex;            
        public float startRealtime;      
        public int exit_index;      
    }

    static LevelCtx _level;

    // ===== Public API =====

    /// <summary>
    /// Gọi khi người chơi bắt đầu 1 LEVEL (not step).
    /// </summary>
    /// <param name="levelName">Tên/ID level (dùng để tính play_index)</param>
    /// <param name="customer">Phân loại khách hàng: "normal_0", "vip_350", ...</param>
    /// <param name="mode">Mode chơi của level (spray | acrylic | halloween | colorful | popit ...)</param>
    /// <param name="totalSteps">Tổng số step có trong level</param>
    public static void LevelStart(int totalSteps)
    {
        // play_index: số lần chơi level này
        int playIndex = PlayerPrefs.GetInt(KeyAttempt(CurrentMode.ToString()), 0) + 1;
        PlayerPrefs.SetInt(KeyAttempt(CurrentMode.ToString()), playIndex);
        PlayerPrefs.Save();

        IncrementTotalPlayCount();
        _level.exit_index = 0;
        GameState.State = State.RUNNING;
        _level = new LevelCtx
        {
            active = true,
            customer = CurrentCustomer,
            mode = CurrentMode,
            totalSteps = totalSteps,
            clearedSteps = 0,
            playIndex = playIndex,
            startRealtime = Time.realtimeSinceStartup,
            exit_index = 0,
        };

        // ====== LEVEL_START ======
      /*  FirebaseEvent.LogEvent("level_start", new Parameter[]
        {
            new Parameter("customer", CurrentCustomer),
            new Parameter("mode", CurrentMode.ToString()),
            new Parameter("play_index", playIndex.ToString())
        });
        
        LogHelper.LogYellow("level start: " + CurrentCustomer + ", " + CurrentMode + ", " + playIndex);*/
    }

    /// <summary>
    /// Gọi khi pass 1 step trong level (để tăng cleared_steps). Tùy bạn gọi ở StepBase.Done()…
    /// </summary>
    public static void LevelStepCleared()
    {
      /*  if (_level.active)
            _level.clearedSteps = Mathf.Clamp(_level.clearedSteps + 1, 0, Mathf.Max(_level.totalSteps, int.MaxValue));*/
    }
    public static void LogLevelUp(int level)
    {
      //  FirebaseEvent.LogEvent("level_up", new Parameter("level", level.ToString()));
       // FirebaseAnalytics.SetUserProperty("player_level", level.ToString());
    }

    public static void ItemPick(string step, int pickId)
    {
       /* var pars = new List<Parameter>
        {
            new Parameter("step", step),
            new Parameter("pick_id", pickId.ToString())
        };
        FirebaseEvent.LogEvent("rw_item", pars.ToArray());
        LogHelper.LogYellow("item pick: " + step + ", " + pickId);*/
    }

    // ===== Helpers =====
    private static int IncrementTotalPlayCount()
    {
        int total = PlayerPrefs.GetInt(KEY_TOTAL_PLAY, 0) + 1;
        PlayerPrefs.SetInt(KEY_TOTAL_PLAY, total);
        PlayerPrefs.Save();

      //  FirebaseAnalytics.SetUserProperty("play_count", total.ToString());
        return total;
    }

    public static void LevelEndInternal(string result)
    {
        // Nếu đang ở trong ads thì chốt luôn phần thời gian ads
        float rawDuration = Time.realtimeSinceStartup - _level.startRealtime;
        float playDuration = Mathf.Max(0f, rawDuration);

       
        GameState.State = State.NO_ACTION;
       /* FirebaseEvent.LogEvent("level_end", pars.ToArray());
        LogHelper.LogYellow("level end: " + _level.customer + ", " 
                            + _level.mode + ", " 
                            + _level.playIndex + ", " 
                            + playDuration.ToString("0.###") + ", " 
                            + _level.totalSteps + ", " 
                            + _level.clearedSteps);*/
        _level = default;
    }
    
    public static void LevelExit()
    {
        float rawDuration = Time.realtimeSinceStartup - _level.startRealtime;
        float playDuration = Mathf.Max(0f, rawDuration);
        _level.exit_index += 1;
      
/*        FirebaseEvent.LogEvent("level_exit", pars.ToArray());
        LogHelper.LogYellow("level exit: " 
                            + _level.customer + ", " 
                            + _level.mode + ", " 
                            + _level.playIndex + ", " 
                            + playDuration.ToString("0.###") + ", " 
                            + _level.totalSteps + ", " 
                            + _level.clearedSteps + ", " 
                            + _level.exit_index);*/
        _level = default;
    }

    public static void TutorialAction(ActionTut action)
    {
        if(PlayerPrefs.GetInt("Tutorial") != 0) return;
        string action_name = action.ToString();
        int action_index = (int) action;
      
    /*    
        FirebaseEvent.LogEvent("tutorial_action", pars.ToArray());
        LogHelper.LogYellow("tutorial_action: " + action_name + ", " + action_index);*/
    }
    
    
}

[System.Serializable]
public enum ActionTut
{
    start = 0,
    finish = 1,
    spray_art = 2,
    complete_scan = 3,
    spray_end = 4,
    dry_start = 5,
    complete_dryer = 6,
    dry_end = 7,
    claim_X2 = 8,
    no_thanks = 9,
    unlock_future_start = 10,
    tap_to_continue = 11,
    tap_to_continue_2 = 12,
    unlock_future_end = 13,
    home_show = 14
}
