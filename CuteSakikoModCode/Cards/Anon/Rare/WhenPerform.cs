
using CuteSakikoMod.CuteSakikoModCode.Enchantments;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;


namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare;

public class WhenPerform() : CuteAnonCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    // 没有动态变量，不重写 CanonicalVars

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            HoverTipFactory.FromEnchantment<PlayEnchantment>();
                yield return HoverTipFactory.FromPower<WhenPerformPower>();
        }
    }
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();
        await PowerCmd.Apply<WhenPerformPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 2 → 1
    }
}