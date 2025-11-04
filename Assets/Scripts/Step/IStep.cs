using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IStep
{
    Sprite Icon { get; }
    void SetUp(PhoneCase phoneCase);
    void CompleteStep();
    
}