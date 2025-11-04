using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StepChooseCasePopIt : StepBase
{
    [SerializeField] private Transform spawnItem;
    [SerializeField] private PopitCasePreview popitCasePreviewPrefab;
    [SerializeField] private List<PopitCasePreview> popitCasePreviews = new List<PopitCasePreview>();
    
    public override void SetUp(PhoneCase phoneCase)
    {
        BuildUIListAndSelectFirst();
    }

    public override void CompleteStep()
    {
        
    }
    
    private void BuildUIListAndSelectFirst()
    {
        popitCasePreviews.Clear();
        var manager = ItemDataManager.Instance.listPopitCase;
        
        foreach (var data in manager)
            if (data.isUnlock && !UserGameData.IsItemChooseCaseUnlocked(data.id))
                UserGameData.UnlockItemChooseCase(data.id);
        
        for (int i = 0; i < manager.Count; i++)
        {
            var casePopit = Instantiate(popitCasePreviewPrefab, spawnItem);
            casePopit.SetUp(manager[i], () => { OnSelectPopitCase(casePopit); });
            casePopit.SetUpUnlock(UserGameData.IsItemChooseCaseUnlocked(casePopit.PopitCaseData.id));
            popitCasePreviews.Add(casePopit);
        }
        if (popitCasePreviews.Count > 0)
            OnSelectPopitCase(popitCasePreviews[0]);
    }

    private void OnSelectPopitCase(PopitCasePreview p)
    {
        if (!p.IsUnlock)
        {
       /*     CallAdsManager.ShowRewardVideo("reward", () =>
            {*/
                UserGameData.UnlockItemChooseCase(p.PopitCaseData.id);
                Gameplay.instance.SpawnPopitCase(p.PopitCaseData.casePopit);
           // });
            return;
        }
        
        Gameplay.instance.SpawnPopitCase(p.PopitCaseData.casePopit);
    }
    
}
