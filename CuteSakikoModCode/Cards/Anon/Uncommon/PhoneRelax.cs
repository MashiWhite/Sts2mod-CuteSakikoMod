using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon;

public class PhoneRelax() : CuteAnonCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new CardsVar(1);
            yield return new EnergyVar(1); // 用于描述中的能量图标
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        var drawCount = DynamicVars["Cards"].IntValue;

        // 立刻抽牌
        await CardPileCmd.Draw(choiceContext, drawCount, Owner);

        // 下回合额外获得 1 点能量（新 API 需要 choiceContext）
        await PowerCmd.Apply<EnergyNextTurnPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);

        // 下回合额外抽牌（数量与本次抽牌数相同）
        await PowerCmd.Apply<DrawCardsNextTurnPower>(choiceContext, Owner.Creature, drawCount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Cards"].UpgradeValueBy(1m); // 1 → 2
        DynamicVars["Energy"].UpgradeValueBy(1m); // 1 → 2（仅描述之用，实际能量始终为1）
    }
}