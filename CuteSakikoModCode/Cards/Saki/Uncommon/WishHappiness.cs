using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

[Pool(typeof(CuteSakiCardPool))]
public class WishHappiness() : CustomCardModel(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
{
    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

    // 多人游戏限定
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    // 动态变量：能量和抽牌数（基础1，升级2）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1),
        new CardsVar(1)
    ];

    // 悬停提示（显示能量和压力）
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return EnergyHoverTip;
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        var targetPlayer = cardPlay.Target.Player;

        // 为目标玩家增加能量和抽牌
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, targetPlayer);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, targetPlayer);

        // 双方增加压力（2层）
        const int pressureAmount = 2;

        // 自身增加压力
        await PowerCmd.Apply<PressurePower>(Owner.Creature, pressureAmount, Owner.Creature, this);

        // 如果目标玩家不是自己，则也给目标玩家增加压力
        if (targetPlayer != Owner)
            await PowerCmd.Apply<PressurePower>(targetPlayer.Creature, pressureAmount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级：能量+1（1→2），抽牌+1（1→2）
        DynamicVars.Energy.UpgradeValueBy(1m);
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}