
using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Rare;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class OblivionisPower : CuteSakikoModPower
{

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    // 每当有牌被消耗时（包括自己？为了避免无限循环，排除自身）
    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card,
        bool causedByEthereal)
    {
        // 忽略能力牌自身被消耗的情况（避免无限循环）
        if (card is Oblivionis) return;

        // 对敌方全体造成8点伤害
        await DamageCmd.Attack(8m)
            .FromCard(card) // 伤害来源为被消耗的牌
            .TargetingAllOpponents(Owner.CombatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }
}