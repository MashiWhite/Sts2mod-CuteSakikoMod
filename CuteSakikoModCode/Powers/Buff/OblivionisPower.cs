using CuteSakikoMod.CuteSakikoModCode.Singletons;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class OblivionisPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    // 静态构造器：确保全局事件只订阅一次
    static OblivionisPower()
    {
        MemoryCardPileManager.OnForgottenCards += OnForgottenCardsGlobal;
    }

    private static async Task OnForgottenCardsGlobal(
        PlayerChoiceContext choiceContext,
        IReadOnlyList<CardModel> forgottenCards,
        CardModel? source)
    {
        if (forgottenCards == null || forgottenCards.Count == 0)
            return;

        // 获取当前战斗状态
        // 方法：从任意一张被遗忘卡的 Owner 获取 CombatState（如果 Owner 存在且处于战斗中）
        ICombatState? combatState = null;
        var firstCardOwner = forgottenCards.FirstOrDefault()?.Owner;
        if (firstCardOwner != null && firstCardOwner.Creature.CombatState != null)
            combatState = firstCardOwner.Creature.CombatState;
        else
        {
            // 降级：从 RunState 的当前房间获取
            var runState = IRunState.GetFrom(forgottenCards.Select(c => c.Owner?.Creature).OfType<Creature>());
            if (runState?.CurrentRoom is CombatRoom combatRoom)
                combatState = combatRoom.CombatState;
        }

        if (combatState == null)
            return;

        // 遍历所有玩家，对每个拥有 OblivionisPower 的玩家造成伤害（每个玩家独立结算）
        foreach (var player in combatState.Players)
        {
            var power = player.Creature.GetPower<OblivionisPower>();
            if (power == null || power.Amount <= 0)
                continue;

            int damagePerCard = power.Amount;
            var enemies = combatState.HittableEnemies;
            if (enemies.Count == 0)
                continue;

            // 每张被遗忘的卡，对所有敌人造成一次伤害
            foreach (var _ in forgottenCards)
            {
                await CreatureCmd.Damage(
                    choiceContext,
                    enemies,
                    damagePerCard,
                    ValueProp.Unpowered,    // 或根据需求调整
                    player.Creature,       // 伤害来源
                    null);                 // 无卡牌来源
            }
        }
    }

    // 实例方法不再需要订阅/取消订阅
    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);
        // 无需订阅
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        // 无需取消订阅
        await base.AfterRemoved(oldOwner);
    }
}