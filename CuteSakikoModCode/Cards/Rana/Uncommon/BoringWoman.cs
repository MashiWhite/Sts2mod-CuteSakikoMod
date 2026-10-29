using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Uncommon;

public class BoringWoman : CuteRanaCard
{
    public BoringWoman() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<StrengthPower>(1M),
        new PowerVar<DexterityPower>(1M)
    };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<StrengthPower>();
            yield return HoverTipFactory.FromPower<DexterityPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // ⚠️ PowerVar<T> 的 key 是**类型名全称**（DexterityPower / StrengthPower），
        //    不是属性短名（Dexterity / Strength）。写成短名会 KeyNotFoundException，
        //    图鉴渲染升级预览时就会崩（2026-10-29 实测）。
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature, DynamicVars["StrengthPower"].BaseValue,
            Owner.Creature, this);
        await PowerCmd.Apply<DexterityPower>(choiceContext, Owner.Creature, DynamicVars["DexterityPower"].BaseValue,
            Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["DexterityPower"].UpgradeValueBy(2M); // 敏捷 1 → 3
    }
}