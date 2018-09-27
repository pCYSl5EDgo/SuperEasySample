using TMPro;
using Unity.Entities;
using Unity.Jobs;

[AlwaysUpdateSystem]
sealed class CountUpSystem : ComponentSystem
{
    readonly TMP_Text countDownText;
    ComponentGroup g;
    uint cachedCount = 0;
    
    public CountUpSystem(TMP_Text countDownText) => this.countDownText = countDownText;
    protected override void OnCreateManager() => g = GetComponentGroup(ComponentType.ReadOnly<Count>());

    protected override void OnUpdate()
    {
        uint current = (uint)g.CalculateLength();
        if (current == cachedCount) return;
        cachedCount = current;
        countDownText.text = cachedCount.ToString();
    }
}