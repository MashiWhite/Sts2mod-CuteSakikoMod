using CuteSakikoMod.CuteSakikoModCode.Singletons;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using System.Reflection;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Mod.Curse;

public class Flyback : ModTokenCard
{
    // 记录升级加成的总量（不受 reloads 影响）
    private int _upgradeDamageBonus = 0;
    private int _upgradeDrawBonus = 0;

    public Flyback() : base(0, CardType.Attack, CardRarity.Ancient, TargetType.AnyEnemy)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Retain };

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            int reloads = GetReloadCount();
            // 基础值最低 3 伤害，1 抽牌，升级部分后续单独加上
            int baseDamage = Math.Max(3, 8 - reloads);
            int baseDraw = Math.Max(1, 2 - (reloads / 3));
            
            yield return new DamageVar(baseDamage, ValueProp.Move);
            yield return new CardsVar(baseDraw);
        }
    }

    // 每次 play 前刷新动态变量，让数值跟上最新的 reloads
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RefreshDynamicVars(); // 保证伤害 / 抽牌计算正确

        FlybackManager.Instance.IncrementPlayCountForPlayer(cardPlay.Card.Owner);

        if (cardPlay.Target != null)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
    }

    protected override void OnUpgrade()
    {
        _upgradeDamageBonus += 3;
        _upgradeDrawBonus   += 1;

        // 升级时直接增加 BaseValue，同时触发绿色高亮
        if (DynamicVars.TryGetValue("Damage", out var dmgVar))
            dmgVar.UpgradeValueBy(3);
        if (DynamicVars.TryGetValue("Cards", out var cardsVar))
            cardsVar.UpgradeValueBy(1);
    }

    // 根据 reloads 和升级加成重新计算当前数值
    private void RefreshDynamicVars()
    {
        int reloads = GetReloadCount();
        int finalDamage = Math.Max(3, 8 - reloads) + _upgradeDamageBonus;
        int finalDraw   = Math.Max(1, 2 - (reloads / 3)) + _upgradeDrawBonus;

        if (DynamicVars.TryGetValue("Damage", out var dmgVar))
            dmgVar.BaseValue = finalDamage;
        if (DynamicVars.TryGetValue("Cards", out var cardsVar))
            cardsVar.BaseValue = finalDraw;
    }

    private static int GetReloadCount()
    {
        var field = typeof(RunManager).GetField("_numReloads", BindingFlags.NonPublic | BindingFlags.Instance);
        return field != null ? (int)field.GetValue(RunManager.Instance) : 0;
    }
}