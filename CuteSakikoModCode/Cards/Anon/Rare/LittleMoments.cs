using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Token;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare;

public class LittleMoments() : CuteAnonCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromCard<Lifetime>(IsUpgraded); }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new CardsVar(2); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);

        if (CombatState != null && Owner != null)
        {
            var copy = CombatState.CreateCard<LittleMoments>(Owner);
            if (IsUpgraded)
            {
                copy.UpgradeInternal();
                copy.FinalizeUpgradeInternal();
            }

            await CardPileCmd.Add(copy, PileType.Discard, CardPilePosition.Top);
        }

        await Cmd.CustomScaledWait(0.1f, 0.15f);
        // 合成逻辑已移至 LittleMomentsManager 单例中实时检测
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}