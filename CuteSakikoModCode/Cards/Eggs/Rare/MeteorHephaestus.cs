using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Rare;

public class MeteorHephaestus() : CuteSakikoModEggCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;


    protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<MeteorHephaestusPower>(); }
    }


    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = Owner.Creature.CombatState;
        var selfPower = Owner.Creature.GetPower<MeteorHephaestusPower>();
        var otherHas = combatState != null &&
                       combatState.Creatures.Any(c =>
                           c != Owner.Creature && c.GetPower<MeteorHephaestusPower>() != null);

        if (otherHas)
        {
            // 其他队友已有该能力，改为回复2能量，抽1张牌
            await PlayerCmd.GainEnergy(2, Owner);
            await CardPileCmd.Draw(choiceContext, 1, Owner);
        }
        else
        {
            if (selfPower == null)
            {
                var layers = IsUpgraded ? 2 : 1;
                var power = await PowerCmd.Apply<MeteorHephaestusPower>(choiceContext, Owner.Creature, layers,
                    Owner.Creature, this);
                power.SetUpgraded(IsUpgraded);
            }
            else
            {
                var add = IsUpgraded ? 2 : 1;
                await PowerCmd.ModifyAmount(choiceContext, selfPower, add, Owner.Creature, this);
                // 如果当前卡牌是升级版，确保能力的升级标志为 true
                if (IsUpgraded) selfPower.SetUpgraded(true);
            }
        }
    }

    protected override void OnUpgrade()
    {
        // 升级效果在层数增加中体现
    }
}