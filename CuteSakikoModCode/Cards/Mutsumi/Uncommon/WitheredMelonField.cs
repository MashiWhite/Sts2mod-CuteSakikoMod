using CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Archetypes;
using CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Planting;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Uncommon;

/// <summary>
///     枯萎的瓜田（小睦/Cucumber 蓝卡）：1c 能力，虚无。
///     失去 1 个种植槽，获得 3 点敏捷；升级后 4 点敏捷。
/// </summary>
public class WitheredMelonField()
    : CuteMutsumiCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self), IMutsumiArchetypeCard
{
    public MutsumiArchetype Archetype => MutsumiArchetype.Cucumber;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<DexterityPower>(3m)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<DexterityPower>();

            // canonical 卡（图鉴/预览）没有 Owner，读它会抛 CanonicalModelException，
            // 导致悬浮框创建失败；这里按 canonical 传 null。
            yield return PlantingKeywordTip.Create(IsCanonical ? null : Owner);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<WitheredMelonFieldPower>(
            choiceContext,
            Owner.Creature,
            1m,
            Owner.Creature,
            this);

        await PowerCmd.Apply<DexterityPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["DexterityPower"].IntValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["DexterityPower"].UpgradeValueBy(1m);
    }
}
