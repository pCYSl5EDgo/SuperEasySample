using TMPro;

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Entities;

public class Manager_CountUp : MonoBehaviour
{
    [SerializeField] TMP_Text countText;
    void Start()
    {
        World.Active = new World("DEFAULT");
        World.Active.CreateManager(typeof(CountUpSystem), countText);
        World.Active.CreateManager(typeof(ClickSystem));
        ScriptBehaviourUpdateOrder.UpdatePlayerLoop(World.Active);
    }
}
