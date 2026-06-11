using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Starter;

public sealed class PostItNote : KabutoNote
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    public override async Task AfterSideTurnStart(
        CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Creature.Side) return;

        // 第一回合
        if (combatState.RoundNumber == 1)
        {
            // 给自己施加 5 层压力
            int powerCount = Math.Min(5, Owner.Creature.CurrentHp-1);
            await PowerCmd.Apply<PressurePower>(
                new ThrowingPlayerChoiceContext(), Owner.Creature, powerCount, Owner.Creature, null);

            // 确保记忆牌堆已初始化（防止读档时牌堆为空）
            await MemoryCardPile.EnsureInitializedAsync(Owner);

            // 随机获取一张记忆卡牌，并升级
            var canonicalCards = MemoryCardPile.GetCanonicalCards(Owner);
            if (canonicalCards.Count > 0)
            {
                var shuffled = canonicalCards.UnstableShuffle(Owner.RunState.Rng.Shuffle);
                var template = shuffled.FirstOrDefault();
                if (template != null)
                {
                    var mutableCard = Owner.Creature.CombatState.CreateCard(template, Owner);
                    mutableCard.UpgradeInternal();
                    mutableCard.FinalizeUpgradeInternal();
                    await CardPileCmd.AddGeneratedCardToCombat(mutableCard, PileType.Hand, Owner);
                }
            }

            Flash();
        }

        // 给所有可攻击的敌人施加 5 层压力（原有效果，每回合都执行）
        if (combatState.HittableEnemies != null)
            foreach (var enemy in combatState.HittableEnemies)
                await PowerCmd.Apply<PressurePower>(
                    new ThrowingPlayerChoiceContext(), enemy, 5, Owner.Creature, null);
    }
}