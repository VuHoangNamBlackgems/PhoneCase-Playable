using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class IconProgression : MonoBehaviour
{
    [SerializeField] STEP _step;
    [SerializeField] Image _icon;
    [SerializeField] Transform _selected;
    private Image img;
    [SerializeField] Sprite sprDone;
    private bool isDone = false;
    public bool IsDone
    {
        get
        {
            return isDone;
        }
        set
        {
            isDone = value;
        }
    }

    public STEP Step
    {
        get => _step;
        set => _step = value;
    }

    private void Awake()
    {
        img = GetComponent<Image>();
    }

    public void SetIcon(Sprite icon)
    {
        _icon.sprite = icon;
        _selected.gameObject.SetActive(false);
        _icon.SetNativeSize();
    }

    public void Selected()
    {
        _selected.gameObject.SetActive(true);
        transform.DOScale(1.12f, 0.3f).SetEase(Ease.InQuad);
    }
    
    public void UnSelected()
    {
        _selected.gameObject.SetActive(false);
        transform.DOScale(1f, 0.3f).SetEase(Ease.OutQuad);
        Done();
    }
    
    public void Done()
    {
        if(isDone) img.sprite = sprDone;
    }
}
