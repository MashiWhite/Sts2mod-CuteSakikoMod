using CuteSakikoMod.CuteSakikoModCode.Cards.Mod;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Token;

public class KnightSword : ModTokenCard
{
    public KnightSword() : base(2, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
    {
    }

    [SavedProperty] private int ExtraDamage { get; set; }

    /// <summary>
    ///     「本次打出额外重放 N 次」的临时次数。由 ByMyReign（唯我独尊）在打出前标记、
    ///     打出后清零。走官方 ModifyCardPlayCount 而非直接改 BaseReplayCount：
    ///     不改卡面属性，因此不会出现「打出过程异常 → BaseReplayCount 永久 +1」的泄漏。
    /// </summary>
    private int _pendingExtraPlays;

    /// <summary>让这张剑下一次打出时额外重放 N 次（仅本次生效）。</summary>
    public void QueueExtraPlays(int times) => _pendingExtraPlays = Math.Max(0, times);

    /// <summary>清掉待消费的额外重放（打出后必须调用，防止泄漏）。</summary>
    public void ClearExtraPlays() => _pendingExtraPlays = 0;

    /// <summary>
    ///     官方重放入口：本次打出把 pending 次数加到 playCount 上。
    ///     ⚠️ 这里**不**自减 pending —— 由调用方在 AutoPlay 结束后显式 ClearExtraPlays。
    /// </summary>
    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (!ReferenceEquals(card, this)) return playCount;
        return _pendingExtraPlays > 0 ? playCount + _pendingExtraPlays : playCount;
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Retain];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.KnightSword.GetModCardKeyword());
            yield return HoverTipFactory.FromPower<BreakDownPower>();
            yield return HoverTipFactory.FromPower<PressurePower>();
        }
    }

    public override TargetType TargetType => HasSweepPower ? TargetType.AllEnemies : base.TargetType;

    private bool HasSweepPower => IsMutable && Owner != null && Owner.Creature.HasPower<SakiSweepPower>();

    // ★ 在克隆/降级/读档完成后恢复额外伤害
    protected override void AfterCloned()
    {
        base.AfterCloned();
        if (ExtraDamage > 0)
            DynamicVars.Damage.BaseValue += ExtraDamage;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (HasSweepPower)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .TargetingAllOpponents(CombatState)
                .WithHitFx("vfx/vfx_giant_horizontal_slash", tmpSfx: "slash_attack.mp3")
                .Execute(choiceContext);
        }
        else
        {
            if (cardPlay.Target != null)
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this, cardPlay)
                    .Targeting(cardPlay.Target)
                    .WithHitFx("vfx/vfx_giant_horizontal_slash", tmpSfx: "slash_attack.mp3")
                    .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars.Damage.UpgradeValueBy(5);
        // 不再手动恢复，AfterCloned 会处理
    }

    // 不再需要 AfterDowngraded

    public static void IncreaseDamage(int delta, CombatState combatState)
    {
        if (delta <= 0) return;
        foreach (var player in combatState.Players)
        foreach (var pileType in new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust })
        {
            var pile = pileType.GetPile(player);
            if (pile == null) continue;
            foreach (var card in pile.Cards)
                if (card is KnightSword ks)
                {
                    ks.ExtraDamage += delta;
                    ks.DynamicVars.Damage.BaseValue += delta;
                }
        }
    }
}