using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    private List<Pair<AudioSource, float>> sfxList = new List<Pair<AudioSource, float>>();

    private List<Pair<AudioSource, float>> musicList = new List<Pair<AudioSource, float>>();
    
    float menuMusicVolume = 1f;
    
    float menuSoundVolume = 1f;
    
    public AudioSource MenuMusicSource;
    
    public AudioSource Click;
    
    public AudioSource Back;
    
    public AudioSource Complete;
    
    public AudioSource VFX;
    
    public AudioSource Spray;
    
    public AudioSource Acrylic;
    
    public AudioSource Drier;
    
    public AudioSource Glue;
    
    public AudioSource Glitter;
    
    public AudioSource DoorOpen;
    
    public AudioSource KeyChain;
    
    public AudioSource Pop;
    
    public AudioSource Flip;
    
    public AudioSource Screw;
    
    public AudioSource Peel;
    
    public AudioSource ScreenOut;


    private void Awake()
    {
        _instance = this;
        DontDestroyOnLoad(this);
    }

    void Initialized()
    {
        DontDestroyOnLoad(this);
        
        musicList.Add(new Pair<AudioSource, float>(MenuMusicSource, menuMusicVolume));
        
        sfxList.Add(new Pair<AudioSource, float>(Complete, menuSoundVolume));
        sfxList.Add(new Pair<AudioSource, float>(Click, menuSoundVolume));
    }

    public bool SfxIsOn;



    public bool MusicIsOn;
   

    public void PlayMusic()
    {
        MenuMusicSource?.Play();
    }

    public void StopMusic()
    {
        MenuMusicSource?.Stop();
    }

    public void PlayClick()
    {
        Click?.Play();
    }
    
    public void PlayBack()
    {
        Back?.Play();
    }
    
    public void PlayComplete()
    {
        Complete?.Play();
    }
    
    public void PlaySpray()
    {
        Spray?.Play();
    }

    public void StopSpray()
    {
        Spray?.Stop();
    }
    
    public void PlayAcrylic()
    {
        Acrylic?.Play();
    }

    public void StopAcrylic()
    {
        Acrylic?.Stop();
    }
    
    public void PlayGlue()
    {
        Glue?.Play();
    }

    public void StopGlue()
    {
        Glue?.Stop();
    }
    
    public void PlayDrier()
    {
        Drier?.Play();
    }

    public void StopDrier()
    {
        Drier?.Stop();
    }
    
    public void PlayGlitter()
    {
        Glitter?.Play();
    }

    public void StopGlitter()
    {
        Glitter?.Stop();
    }
    
    public void PlayVFX()
    {
        VFX?.Play();
    }

    public void PlayDoorOpen()
    {
        DoorOpen?.Play();
    }
    
    public void PlayPop()
    {
        Pop?.Play();
    }
    
    public void PlayFlip()
    {
        Flip?.Play();
    }
    
    public void PlayScrew()
    {
        //Screw?.Play();
    }
    
    public void PlayPeel()
    {
        Peel?.Play();
    }
    
    public void StopPeel()
    {
        Peel?.Stop();
    }
    
    public void PlayScreenOut()
    {
        ScreenOut?.Play();
    }
    
    public void StopScreenOut()
    {
        ScreenOut?.Stop();
    }
    
    public void StopAll(bool stopSfx, bool stopMusic)
    {
        if (sfxList.Count == 33) sfxList.Remove(sfxList[32]);
        if (stopSfx)
        {
            foreach (Pair<AudioSource, float> sfx in sfxList)
            {
                sfx.First.Stop();
            }
        }

        if (stopMusic)
        {
            foreach (Pair<AudioSource, float> music in musicList)
            {
                music.First.Stop();
            }
        }
    }

    public void PauseAll(bool stopSfx, bool stopMusic)
    {
        if (stopSfx)
        {
            foreach (Pair<AudioSource, float> sfx in sfxList)
            {
                sfx.First.Pause();
            }
        }

        if (stopMusic)
        {
            foreach (Pair<AudioSource, float> music in musicList)
            {
                music.First.Pause();
            }
        }
    }

    public void UnPauseAll(bool stopSfx, bool stopMusic)
    {
        if (stopSfx)
        {
            foreach (Pair<AudioSource, float> sfx in sfxList)
            {
                sfx.First.UnPause();
            }
        }

        if (stopMusic)
        {
            foreach (Pair<AudioSource, float> music in musicList)
            {
                music.First.UnPause();
            }
        }
    }


    private static AudioManager _instance;

    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Instantiate(Resources.Load<AudioManager>("AudioManager"));
                _instance.Initialized();
            }

            return _instance;
        }
    }

    

}