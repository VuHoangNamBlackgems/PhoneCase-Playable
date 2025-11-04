using System;
using System.Collections;
using System.Collections.Generic;
using Coffee.UIExtensions;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class EmojiEndStep : MonoBehaviour
{
    public static EmojiEndStep Intance;
    
    [SerializeField]
    private Sprite[] _listEmojiHappy;

    [SerializeField]
    private Sprite[] _listEmojiSad;

    [SerializeField]
    private Sprite[] _listText;

    [SerializeField]
    private Sprite[] _listTextSad;

    [SerializeField]
    private Image _icEmoji;

    [SerializeField]
    private Image _icText;

    [SerializeField]
    private Animator _animator;

    [SerializeField]
    private ParticleSystem _particle;
    
    [SerializeField] UIParticle particle;

    private float _timeShow;

    private void OnEnable()
    {
        SetEmoji();
    }

    private void SetEmoji()
    {
        var random = Random.Range(0, _listEmojiHappy.Length);
        _icEmoji.sprite = _listEmojiHappy[random];
        _icText.sprite = _listText[random];
        _icText.SetNativeSize();
    }
    public void ShowEmojiHappy(Action callback = null, bool ishow = true)
    {
        SetEmoji();
        _animator.Rebind();
        _animator.Update(0f);
        _animator.Play("Emoji", 0, 0f);
        particle.Play();

    }

    
}
