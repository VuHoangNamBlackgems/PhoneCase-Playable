using System.Runtime.InteropServices;
using UnityEngine;

public static class VibrationManager
{
    public enum HapticImpact { Light = 0, Medium = 1, Heavy = 2 }

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void _iOSVibrateDefault();
    [DllImport("__Internal")] private static extern void _iOSImpact(int style);   // 0,1,2
#endif

    // Rung 1 nhịp (ms: thời lượng, amplitude: 1..255 Android, -1 = mặc định)
    public static void Vibrate(long ms = 30, int amplitude = -1)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity    = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            var context     = activity.Call<AndroidJavaObject>("getApplicationContext");
            var vibrator    = context.Call<AndroidJavaObject>("getSystemService", "vibrator");
            if (vibrator == null) return;

            int api = new AndroidJavaClass("android.os.Build$VERSION").GetStatic<int>("SDK_INT");
            if (api >= 26)
            {
                var vibEff = new AndroidJavaClass("android.os.VibrationEffect");
                AndroidJavaObject effect =
                    amplitude >= 0
                    ? vibEff.CallStatic<AndroidJavaObject>("createOneShot", ms, amplitude)
                    : vibEff.CallStatic<AndroidJavaObject>("createOneShot", ms, vibEff.GetStatic<int>("DEFAULT_AMPLITUDE"));
                vibrator.Call("vibrate", effect);
            }
            else
            {
                vibrator.Call("vibrate", ms);
            }
        }
        catch { /* ignore */ }
#elif UNITY_IOS && !UNITY_EDITOR
        _iOSVibrateDefault();
#else
        Handheld.Vibrate();
#endif
    }

    // Rung theo pattern (Android). timings = [delay, on, off, on, off...]; amplitudes = cường độ tương ứng (1..255)
    public static void Pattern(long[] timings, int[] amplitudes = null, int repeat = -1)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity    = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            var context     = activity.Call<AndroidJavaObject>("getApplicationContext");
            var vibrator    = context.Call<AndroidJavaObject>("getSystemService", "vibrator");
            if (vibrator == null) return;

            int api = new AndroidJavaClass("android.os.Build$VERSION").GetStatic<int>("SDK_INT");
            if (api >= 26)
            {
                var vibEff = new AndroidJavaClass("android.os.VibrationEffect");
                AndroidJavaObject effect = (amplitudes != null && amplitudes.Length == timings.Length)
                    ? vibEff.CallStatic<AndroidJavaObject>("createWaveform", timings, amplitudes, repeat)
                    : vibEff.CallStatic<AndroidJavaObject>("createWaveform", timings, repeat);
                vibrator.Call("vibrate", effect);
            }
            else
            {
                vibrator.Call("vibrate", timings, repeat);
            }
        }
        catch { /* ignore */ }
#else
        // iOS/Editor: bỏ qua hoặc dùng Vibrate() fallback
        Vibrate();
#endif
    }

    // Haptic “đúng điệu” (Impact) – Light/Medium/Heavy
    public static void Impact(HapticImpact type = HapticImpact.Light)
    {
#if UNITY_IOS && !UNITY_EDITOR
        _iOSImpact((int)type);
#else
        // Android: map sang rung ngắn với biên độ tương ứng
        int amp = type == HapticImpact.Light ? 40 : (type == HapticImpact.Medium ? 120 : 200);
        Vibrate(20, amp);
#endif
    }
}
