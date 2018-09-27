using TMPro;

using UnityEngine;

using Unity.Entities;

sealed class Manager_CountUp : MonoBehaviour
{
    [SerializeField] TMP_Text countText;
    void Start()
    {
        var world = World.Active = new World("count up");
        world.CreateManager(typeof(CountUpSystem), countText);
        world.CreateManager(typeof(ClickSystem));
        ScriptBehaviourUpdateOrder.UpdatePlayerLoop(world);
    }
}