
using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models; // 提供 ModelDb.AllCards
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Common;

public class RoughSketch : CuteAnonRelic
{
    public override RelicRarity Rarity => RelicRarity.Common;

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        // 只在自己阵营首回合触发
        if (side != Owner.Creature.Side || combatState.RoundNumber != 1)
            return;

        // 从整个游戏的所有和弦牌中随机选择一张（规范模板）
        var allChordCards = ModelDb.AllCards
            .Where(c => c.HasModKeyword(CutesakiKeywords.Chord))
            .ToList();

        if (allChordCards.Count == 0) return;

        var template = Owner.RunState.Rng.CombatCardSelection.NextItem(allChordCards);
        if (template == null) return;

        // 创建战斗用副本，添加虚无，加入手牌
        var newCard = combatState.CreateCard(template, Owner);
        newCard.AddKeyword(CardKeyword.Ethereal);
        await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, Owner);

        Flash();
    }
}