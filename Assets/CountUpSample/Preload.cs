using UnityEngine;

using Unity.Entities;

static class Preload
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize() => PlayerLoopManager.RegisterDomainUnload(DomainUnloadShutdown, 10000);

    static void DomainUnloadShutdown()
    {
        World.DisposeAllWorlds();
        ScriptBehaviourUpdateOrder.UpdatePlayerLoop();
    }
}