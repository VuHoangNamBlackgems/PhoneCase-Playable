using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Order;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Random = UnityEngine.Random;

[DisallowMultipleComponent]
public class CustomerOrderManager : MonoBehaviour
{
    [Serializable]
    public struct OrderEntry
    {
        public int id;
        public Sprite icon;

        public OrderEntry(int dataID, Sprite dataIcon)
        {
            this.id = dataID;
            this.icon = dataIcon;
        }
    }

    [Header("UI Refs")] 
    [SerializeField] private CustomerOrderItem item;
    [SerializeField] private float showStagger = 0.06f;

    [SerializeField] private CustomerOrderReward reward;
    [SerializeField] private List<CustomerOrderSO> listToolGlue = new List<CustomerOrderSO>();
    [SerializeField] private List<CustomerOrderSO> listToolDryer = new List<CustomerOrderSO>();
    [SerializeField] GlueStep glueStep;
    int maxItems = 1;
    int countItem = 0;
    bool isCompleteItem = false;
    public static CustomerOrderManager instance;
    void Awake()
    {
        instance = this;
    }

    void OnEnable()
    {
        
    }

    void OnDisable()
    {
    }

    public void OrderItem(OrderEntry orders, bool playShowAnim = true)
    {
        if(maxItems == countItem) return;
        float delay = 0f;
        isCompleteItem = false;
        countItem = 0;
        var level = UserLevel.Level;
        if (level > 3)
        {
            maxItems = 2;
        }
        else
        {
            maxItems = 1;
        }
        item.gameObject.SetActive(true);
        item.SetupOrder(orders.id, orders.icon);

        if (playShowAnim)
        {
            item.ShowOrder(delay);
            delay += showStagger;
        }
        else
        {
            item.gameObject.SetActive(true);
        }
    }

    public bool CheckOrder(int orderId)
    {
        return orderId == item.RequiredId;
    }

    public void CompleteOrder(int orderId)
    {
        if(maxItems == countItem) return;
        Debug.Log(orderId);
        Debug.Log(CheckOrder(orderId));
        if (CheckOrder(orderId) && !isCompleteItem)
        {
            isCompleteItem = true;
            countItem += 1;
            item.Complete();
        }
    }
    int countToolGlue = 0;
    int countToolDryer = 0;
    public void ShowRewardGlue()
    {
        var tool = listToolGlue[Random.Range(0, listToolGlue.Count)];
        reward.gameObject.SetActive(true);
        reward.SetupTool(tool);
        reward.OnReward += () =>
        {
            countToolGlue += 1;
            glueStep.AdoptExternalTool(tool.Tool.transform);   
        };
        if (reward) reward.Play();
    }
    
    public void HideItem()
    {
        item.gameObject.SetActive(false);
    }

    public void HideReward()
    {
        if (reward) reward.Stop();
        reward.gameObject.SetActive(false);
    }

    public void ClaimReward()
    {
        HideReward();
    }

}