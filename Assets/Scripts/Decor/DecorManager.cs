using System;
using UnityEngine;
using DG.Tweening; 

public class DecorManager : MonoBehaviour
{
    public static DecorManager Instance { get; private set; }
    public event Action<DecorType, int> OnLevelChanged;
    
  //  [Header("UI Elements")]
    [SerializeField] CanvasGroup header;         
    [SerializeField] RectTransform headerRt;
    [SerializeField] RectTransform[] cards;
    
    
   // [Header("Interior (Prefab theo level)")]
    [SerializeField] Transform interiorParent;
    [SerializeField] GameObject[] interiorObjects; 
    [SerializeField] int[] interiorCosts;          

   // [Header("Wall (Material theo level)")]
    [SerializeField] Texture[] wallTextures;
    [SerializeField] Material wallMaterial;     
    [SerializeField] int[] wallCosts;              

  //  [Header("Floor (Material theo level)")]
    [SerializeField] Texture[] floorTextures;
    [SerializeField] Material floorMaterial;    
    [SerializeField] int[] floorCosts;             

    const string KEY_INT   = "DECOR_LEVEL_INTERIOR";
    const string KEY_WALL  = "DECOR_LEVEL_WALL";
    const string KEY_FLOOR = "DECOR_LEVEL_FLOOR";

    int levelInterior, levelWall, levelFloor;
    GameObject currentInterior;

    void Awake() { Instance = this; }
    void Start()
    {
        Load();
        ApplyAll();
        NotifyAll();
    }
    
    void OnEnable()
    {
        float t = 0f;
        if (header)
        {
            header.alpha = 0; headerRt.anchoredPosition += new Vector2(0, -25f);
            header.DOFade(1, 0.25f);
            headerRt.DOAnchorPosY(headerRt.anchoredPosition.y + 25f, 0.35f).SetEase(Ease.OutCubic);
            t = 0.08f;
        }

        for (int i = 0; i < cards.Length; i++)
        {
            var rt = cards[i];
            if (!rt) continue;
            var cg = rt.GetComponent<CanvasGroup>() ?? rt.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0;

            Vector2 start = rt.anchoredPosition + new Vector2(0, -35f);
            rt.anchoredPosition = start;

            rt.localScale = Vector3.one * 0.92f;

            Sequence s = DOTween.Sequence();
            s.PrependInterval(t + i * 0.06f);
            s.Append(cg.DOFade(1, 0.22f));
            s.Join(rt.DOAnchorPosY(start.y + 35f, 0.28f).SetEase(Ease.OutCubic));
            s.Join(rt.DOScale(1.02f, 0.20f).SetEase(Ease.OutBack, 6f)); 
        }
    }

    // ===== Public API =====
    public int GetLevel(DecorType t) =>
        t == DecorType.Interior ? levelInterior : (t == DecorType.Wall ? levelWall : levelFloor);

    public int MaxLevel(DecorType t)
    {
        switch (t)
        {
            case DecorType.Interior:
                return Mathf.Max(1, interiorObjects != null ? interiorObjects.Length : 1);

            case DecorType.Wall:
                return Mathf.Max(1, wallTextures != null ? wallTextures.Length : 1);

            case DecorType.Floor:
                return Mathf.Max(1, floorTextures != null ? floorTextures.Length : 1);

            default:
                return 1;
        }
    }

    // cost để lên level kế tiếp; trả -1 nếu đã max
    public int GetNextCost(DecorType t)
    {
        int lv = GetLevel(t);
        int[] costs = null;

        switch (t)
        {
            case DecorType.Interior:
                costs = interiorCosts;
                break;

            case DecorType.Wall:
                costs = wallCosts;
                break;

            case DecorType.Floor:
                costs = floorCosts;
                break;

            default:
                costs = null;
                break;
        }

        if (costs == null || lv >= costs.Length)
            return -1;

        return Mathf.Max(0, costs[lv]);
    }


    public bool TryUpgrade(DecorType t)
    {
        int lv   = GetLevel(t);
        int max  = MaxLevel(t);
        if (lv >= max - 1) return false; 
        
        int cost = GetNextCost(t);
        if (cost > 0)
        {
            int cash = UserInventory.GetCurrencyValue(CurrencyType.CASH);
            if (cash < cost)
            {
                /*CallAdsManager.ShowRewardVideo("decor_item", () =>
                {*/
                    lv++;
                    if (t == DecorType.Interior) levelInterior = lv;
                    else if (t == DecorType.Wall) levelWall = lv;
                    else levelFloor = lv;

                    Save();
                    Apply(t, lv);
                    OnLevelChanged?.Invoke(t, lv);
              //  });
            }
            else
            {
                UserInventory.ChangeCurrency(CurrencyType.CASH, -cost, true, true);
            }
        }

        lv++;
        if (t == DecorType.Interior) levelInterior = lv;
        else if (t == DecorType.Wall) levelWall = lv;
        else levelFloor = lv;

        Save();
        Apply(t, lv);
        OnLevelChanged?.Invoke(t, lv);
        return true;
    }

    // ===== Internal =====
    void ApplyAll()
    {
        Apply(DecorType.Interior, levelInterior);
        Apply(DecorType.Wall,     levelWall);
        Apply(DecorType.Floor,    levelFloor);
    }

    void Apply(DecorType t, int lv)
    {
        switch (t)
        {
            case DecorType.Interior:
                if (currentInterior) Destroy(currentInterior);
                if (interiorObjects != null && lv < interiorObjects.Length && interiorObjects[lv])
                {
                    var interior = interiorObjects[Mathf.Clamp(lv, 0, interiorObjects.Length - 1)];
                    interior.gameObject.SetActive(true);
                }
                break;

            case DecorType.Wall:
                if (wallTextures != null && wallTextures.Length > 0)
                {
                    var texture = wallTextures[Mathf.Clamp(lv, 0, wallTextures.Length - 1)];
                    wallMaterial.mainTexture = texture;
                }
                break;

            case DecorType.Floor:
                if (floorTextures != null && floorTextures.Length > 0)
                {
                    var texture = floorTextures[Mathf.Clamp(lv, 0, floorTextures.Length - 1)];
                    floorMaterial.mainTexture = texture;
                }
                break;
        }
    }

    void Load()
    {
        levelInterior = PlayerPrefs.GetInt(KEY_INT,   0);
        levelWall     = PlayerPrefs.GetInt(KEY_WALL,  0);
        levelFloor    = PlayerPrefs.GetInt(KEY_FLOOR, 0);
    }

    void Save()
    {
        PlayerPrefs.SetInt(KEY_INT,   levelInterior);
        PlayerPrefs.SetInt(KEY_WALL,  levelWall);
        PlayerPrefs.SetInt(KEY_FLOOR, levelFloor);
    }

    void NotifyAll()
    {
        OnLevelChanged?.Invoke(DecorType.Interior, levelInterior);
        OnLevelChanged?.Invoke(DecorType.Wall,     levelWall);
        OnLevelChanged?.Invoke(DecorType.Floor,    levelFloor);
    }
    
    public bool HasAnyDecorUpgradeable()
    {
        foreach (DecorType type in Enum.GetValues(typeof(DecorType)))
        {
            int cost = GetNextCost(type);
            if (cost > 0 && UserInventory.GetCurrencyValue(CurrencyType.CASH) >= cost)
            {
                return true;
            }
        }
        return false;
    }

    
    public bool HasDecorUpgradeable(DecorType t)
    {
        int cost = GetNextCost(t);
        if (cost > 0 && UserInventory.GetCurrencyValue(CurrencyType.CASH) >= cost)
        {
            return true;
        }
        return false;
    }
}

[System.Serializable]
public enum DecorType
{
    Interior,
    Wall,
    Floor,
}
