using UnityEngine;

[CreateAssetMenu(fileName = "Audio global", menuName = "AudioGlobal")]
public class AudioGlobal : ScriptableObject
{
    public void CallBack_ButtonClick()
    {
        if(AudioManager.Instance) AudioManager.Instance.PlayClick();
    }

    public void CallBack_ButtonBack()
    {
        AudioManager.Instance.PlayBack();
    }
}