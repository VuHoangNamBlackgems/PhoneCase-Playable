using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class SnapScroll : MonoBehaviour
{
    public RectTransform listContent;
    public RectTransform listViewPort;
    public ScrollRect scroll;

    public Button rightArrow;
    public Button leftArrow;

    [Header("Config")] public int maxItem;
    public float itemLength;
    public int itemSpace;

    [Space(15)] private int _currentNumber;

    public int currentNumber
    {
        get { return _currentNumber; }
        set
        {
            if (value > maxItem)
                _currentNumber = maxItem;
            else if (value < 0)
                _currentNumber = 0;
            else
                _currentNumber = value;
        }
    }


    float contentLength;
    float viewPortLength;
    float contentMoveLength;
    Tweener listTween;

    public void SetupList()
    {
        listContent.sizeDelta =
            new Vector2(itemLength * maxItem + (maxItem - 1) * itemSpace, listContent.anchoredPosition.y);

        contentLength = listContent.sizeDelta.x;
        viewPortLength = listViewPort.rect.size.x;
        contentMoveLength = contentLength - viewPortLength;
    }

    public void SetActiveArrowBtn(bool active)
    {
        rightArrow.interactable = active;
        leftArrow.interactable = active;

        if (currentNumber == maxItem - 1)
        {
            rightArrow.interactable = false;
        }

        if (currentNumber == 0)
        {
            leftArrow.interactable = false;
        }
    }

    public void UpdateScroll(bool isMove = true)
    {
        UpdateScroll(currentNumber, isMove);
    }

    public void UpdateScroll(int order, bool isMove = true)
    {
        var localPos = itemLength / 2 + order * (itemLength + itemSpace);
        float newPos;
        if (localPos <= viewPortLength / 2)
        {
            newPos = 0;
        }
        else if (localPos >= contentLength - viewPortLength / 2)
        {
            newPos = 1;
        }
        else
        {
            newPos = (localPos - viewPortLength / 2) / contentMoveLength;
        }

      /*  DOTweenManager.Ins.KillTween(listTween);
        if (isMove)
        {
            listTween = DOTweenManager.Ins.TweenFloatTime(scroll.horizontalNormalizedPosition, newPos
                , 0.5f, f => { scroll.horizontalNormalizedPosition = f; }).SetEase(Ease.OutBack);
        }
        else
        {
            scroll.horizontalNormalizedPosition = newPos;
        }*/
    }
}