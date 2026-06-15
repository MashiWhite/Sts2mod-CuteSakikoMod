using CuteSakikoMod.CuteSakikoModCode.Singletons;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class OblivionisPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

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

        // 获取遗忘卡牌的拥有者（假设所有被遗忘的卡属于同一个玩家）
        var owner = forgottenCards[0].Owner;
        if (owner == null)
            return;

        // 只检查该玩家是否有 OblivionisPower
        var power = owner.Creature?.GetPower<OblivionisPower>();
        if (power == null || power.Amount <= 0)
            return;

        var combatState = owner.Creature.CombatState;
        if (combatState == null)
            return;

        int damagePerCard = power.Amount;

        // 每张被遗忘的卡，对场上所有可攻击的敌人造成一次伤害
        for (int i = 0; i < forgottenCards.Count; i++)
        {
            // 每次循环重新获取敌人列表，避免因敌人死亡导致集合变化
            var enemies = combatState.HittableEnemies;
            if (enemies.Count == 0)
                break;
            
            await CreatureCmd.Damage(
                choiceContext,
                enemies,
                damagePerCard,
                ValueProp.Unpowered,
                owner.Creature,
                null);
        }
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        await base.AfterRemoved(oldOwner);
    }
}