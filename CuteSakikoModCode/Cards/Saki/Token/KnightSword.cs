using CuteSakikoMod.CuteSakikoModCode.Cards.Mod;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
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

    // 使用属性来存储额外伤害，满足 SavedProperty 要求
    [SavedProperty]
    private int ExtraDamage { get; set; }

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Retain, CutesakiKeywords.Sword.GetModCardKeyword()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<BreakDownPower>();
            yield return HoverTipFactory.FromPower<PressurePower>();
        }
    }

    public override TargetType TargetType => HasSweepPower ? TargetType.AllEnemies : base.TargetType;

    private bool HasSweepPower => IsMutable && Owner != null && Owner.Creature.HasPower<SakiSweepPower>();

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
        // 恢复累积的额外伤害
        if (ExtraDamage > 0)
            DynamicVars.Damage.BaseValue += ExtraDamage;
    }

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        if (ExtraDamage > 0)
            DynamicVars.Damage.BaseValue += ExtraDamage;
    }

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