using CuteSakikoMod.CuteSakikoModCode.Singletons;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

/// <summary>
///     星爱音的支配（恐惧模式）：挂在本幕小怪身上的展示型 power。
///     图标复用星爱音的「逆跳」，smartDescription 里的力量数值
///     在局内按 1 + 飞返打出次数% 实时计算（随飞返数据变化刷新）。
/// </summary>
public sealed class StarAnonDominionPower : CuteSakikoModPower
{
    private bool _subscribed;

    public override PowerAssetProfile AssetProfile => new(
        "res://CuteSakikoMod/images/powers/RetrogradePower.png",
        "res://CuteSakikoMod/images/powers/RetrogradePower.png");

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new DynamicVar("StrengthGain", 1); }
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);

        var manager = FlybackManager.Instance;
        if (manager != null)
        {
            manager.OnFlybackDataChanged -= OnFlybackDataChanged;
            manager.OnFlybackDataChanged += OnFlybackDataChanged;
            _subscribed = true;
        }

        UpdateStrengthGain();
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (_subscribed)
        {
            var manager = FlybackManager.Instance;
            if (manager != null) manager.OnFlybackDataChanged -= OnFlybackDataChanged;
            _subscribed = false;
        }

        await base.AfterRemoved(oldOwner);
    }

    private void OnFlybackDataChanged(int playCount, int reloadCount)
    {
        UpdateStrengthGain();
    }

    private void UpdateStrengthGain()
    {
        var count = FlybackManager.Instance?.TotalPlayCount ?? 0;
        DynamicVars["StrengthGain"].BaseValue = 1m + count / 100m;
        InvokeDisplayAmountChanged();
    }
}
