using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

/// <summary>
///     挠！挠！：你每打出一张猫咪，对随机敌人造成 5 点伤害（层数=伤害值）。
/// </summary>
public sealed class NekoScratchPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Neko.GetModCardKeyword()); }
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        if (card.Owner?.Creature != Owner) return;
        if (!card.Keywords.Contains(CutesakiKeywords.Neko.GetModCardKeyword())) return;

        var damage = Amount;
        if (damage <= 0) return;

        var enemies = Owner.CombatState.HittableEnemies;
        if (enemies.Count == 0) return;

        Flash();

        // 随机取一个敌人（战斗卡生成流 RNG，联机各端一致调用一次）
        var rng = Owner.Player.RunState.Rng.CombatCardGeneration;
        var target = enemies[rng.NextInt(enemies.Count)];

        await CreatureCmd.Damage(
            choiceContext,
            target,
            new DamageVar(damage, ValueProp.Unpowered),
            Owner,
            null,
            null
        );
    }
}
