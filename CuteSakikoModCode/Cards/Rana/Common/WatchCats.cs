using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Common;

public class WatchCats : CuteRanaCard
{
    public WatchCats() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Neko.GetModCardKeyword());
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int addCount = IsUpgraded ? 2 : 1;

        // 从卡池中获取所有“猫咪”卡
        var allNekoCards = ModelDb.CardPool<CuteSakikoTokenCardPool>()
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(c => c.Keywords.Contains(CutesakiKeywords.Neko.GetModCardKeyword()))
            .ToList();

        if (allNekoCards.Count == 0) return;

        var combatState = Owner.Creature.CombatState!;
        var rng = Owner.RunState.Rng.CombatCardGeneration;

        // 随机生成猫咪卡并加入手牌（本场战斗 0 费）
        for (int i = 0; i < addCount; i++)
        {
            var template = rng.NextItem(allNekoCards);
            var newCard = combatState.CreateCard(template, Owner);
            newCard.EnergyCost.SetThisCombat(0, true);
            await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, Owner);
        }

        // 下回合额外获得 1 点能量
        await PowerCmd.Apply<EnergyNextTurnPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级效果（猫咪数量）在 OnPlay 中通过 IsUpgraded 判断
    }
}