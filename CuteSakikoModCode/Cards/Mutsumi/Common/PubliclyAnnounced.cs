using CuteSakikoMod.CuteSakikoModCode.Compat;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Common;

/// <summary>
///     公之于众（通用白卡）：0c 技能，消耗。
///     抽到这张牌时：失去所有格挡，获得 2 层言刃；升级后 3 层。
///     主动打出没有效果，只通过 Exhaust 关键词消耗。
/// </summary>
public class PubliclyAnnounced()
    : CuteMutsumiCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<WordBladePower>(2m)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.Block),
        HoverTipFactory.FromPower<WordBladePower>()
    ];

    public override async Task AfterCardDrawn(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool fromHandDraw)
    {
        if (card != this) return;
        if (Owner?.Creature == null) return;

        await DamageCompat.LoseBlock(
            choiceContext,
            Owner.Creature,
            Owner.Creature.Block,
            Owner.Creature);

        await PowerCmd.Apply<WordBladePower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["WordBladePower"].IntValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["WordBladePower"].UpgradeValueBy(1m);
    }
}
