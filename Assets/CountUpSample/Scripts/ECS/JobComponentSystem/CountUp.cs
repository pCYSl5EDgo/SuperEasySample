using TMPro;

using Unity.Entities;
using Unity.Jobs;

[AlwaysUpdateSystem]
sealed class CountUpSystem : JobComponentSystem
{
    TMP_Text countDownText;
    ComponentGroup g;
    uint cachedCount = 0;
    public CountUpSystem(TMP_Text countDownText) => this.countDownText = countDownText;

    protected override void OnCreateManager(int capacity) => g = GetComponentGroup(ComponentType.ReadOnly<Count>());

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        uint current = (uint)g.CalculateLength();
        if (current == cachedCount) return inputDeps;
        cachedCount = current;
        countDownText.text = cachedCount.ToString();
        return inputDeps;
    }
}