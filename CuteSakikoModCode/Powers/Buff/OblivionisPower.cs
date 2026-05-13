using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using CuteSakikoMod.CuteSakikoModCode.Singletons;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class OblivionisPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    // 不再在构造函数中订阅事件

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);
        // 能力被施加到生物上时才订阅（此时 this 是可变实例，Owner 可访问）
        MemoryCardPileManager.OnForgottenCards += HandleForgottenCards;
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        // 能力被移除时取消订阅，防止内存泄漏
        MemoryCardPileManager.OnForgottenCards -= HandleForgottenCards;
        await base.AfterRemoved(oldOwner);
    }

    private async Task HandleForgottenCards(
        PlayerChoiceContext choiceContext,
        IReadOnlyList<CardModel> forgottenCards,
        CardModel? source)
    {
        // 只有持有者是玩家时才生效
        if (Owner?.IsPlayer != true) return;
        if (forgottenCards.Count == 0) return;

        var enemies = Owner.CombatState?.Enemies
            .Where(e => e.IsHittable)
            .ToList();

        if (enemies == null || enemies.Count == 0) return;

        int damagePerCard = Amount;

        // 每张被遗忘的牌，对所有可攻击的敌人造成一次伤害
        foreach (var card in forgottenCards)
        {
            foreach (var enemy in enemies)
            {
                await CreatureCmd.Damage(
                    choiceContext,
                    enemy,
                    damagePerCard,
                    ValueProp.Move,
                    Owner,       // 伤害来源：能力的持有者
                    null);       // 没有卡牌来源
            }
        }
    }
}