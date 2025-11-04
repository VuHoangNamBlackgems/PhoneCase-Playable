using UnityEngine;
using System;

public static class UserLevel
{
    const string KEY = "USER_LEVEL_V1";

    [Serializable]
    class Data { public int level = 0; public int playsAtLevel; }

    static Data _d;
    static Data D => _d ?? (_d = Load());

    static Data Load()
    {
        var json = PlayerPrefs.GetString(KEY, "");
        return string.IsNullOrEmpty(json) ? new Data{ level = 0, playsAtLevel = 0 }
            : JsonUtility.FromJson<Data>(json);
    }
    static void Save() { PlayerPrefs.SetString(KEY, JsonUtility.ToJson(D)); PlayerPrefs.Save(); }

    // ====== API bạn dùng ======
    public static int Level => D.level;
    public static int PlaysAtLevel => D.playsAtLevel;
    public static int RequiredPlays => RequiredPlaysForLevel(D.level);

    // Quy tắc theo bảng
    public static int RequiredPlaysForLevel(int level)
    {
        return (level <= 1) ? 1 : 2; // Lv0..2: 1 màn, từ Lv3: 2 màn
    }

    /// Gọi sau mỗi lần người chơi hoàn thành 1 màn
    /// Trả về số lần level up trong lần cộng này (thường là 0 hoặc 1)
    public static int AddPlay(int n = 1)
    {
        int ups = 0;
        D.playsAtLevel += n;

        // Nếu dư màn thì “carry over” sang level kế (giữ tính công bằng)
        while (D.playsAtLevel >= RequiredPlaysForLevel(D.level))
        {
            D.playsAtLevel -= RequiredPlaysForLevel(D.level);
            D.level++;
            UserTracking.LogLevelUp(D.level);
            ups++;
        }

        Save();
        return ups;
    }

    // Cho UI thanh tiến độ
    public static float Progress01()
        => (float)D.playsAtLevel / Mathf.Max(1, RequiredPlaysForLevel(D.level));

    public static void ResetAll() { _d = new Data(); Save(); }
}