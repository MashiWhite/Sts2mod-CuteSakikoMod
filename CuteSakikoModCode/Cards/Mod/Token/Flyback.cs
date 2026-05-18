using CuteSakikoMod.CuteSakikoModCode.Singletons;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Mod.Token;

public class Flyback : ModTokenCard
{
    // 记录升级加成的总量（不受 reloads 影响）
    private int _upgradeDamageBonus;
    private int _upgradeDrawBonus;

    public Flyback() : base(0, CardType.Attack, CardRarity.Ancient, TargetType.AnyEnemy)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Retain };

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            var reloads = GetReloadCount();
            // 基础值最低 3 伤害，1 抽牌，升级部分后续单独加上
            var baseDamage = Math.Max(3, 10 - reloads);
            var baseDraw = Math.Max(1, 2 - reloads / 3);

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
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        RefreshDynamicVars();
        await base.AfterCardDrawn(choiceContext, card, fromHandDraw);
    }

    // 进入战斗时订阅数据变化（卡牌会被放到抽牌堆，随后可能进入手牌）
    public override async Task AfterCardEnteredCombat(CardModel card)
    {
        if (FlybackManager.Instance != null)
        {
            FlybackManager.Instance.OnFlybackDataChanged += OnFlybackDataChanged;
            RefreshDynamicVars(); // 立刻刷新，保证此时显示的数值已经正确
        }

        await base.AfterCardEnteredCombat(card);
    }

    // 卡牌被移除时取消订阅（包括被消耗、战斗结束）
    public override async Task BeforeCardRemoved(CardModel card)
    {
        if (FlybackManager.Instance != null)
            FlybackManager.Instance.OnFlybackDataChanged -= OnFlybackDataChanged;
        await base.BeforeCardRemoved(card);
    }

    private void OnFlybackDataChanged(int playCount, int reloadCount)
    {
        RefreshDynamicVars();
    }

    protected override void OnUpgrade()
    {
        _upgradeDamageBonus += 6;
        _upgradeDrawBonus += 1;

        // 升级时直接增加 BaseValue，同时触发绿色高亮
        if (DynamicVars.TryGetValue("Damage", out var dmgVar))
            dmgVar.UpgradeValueBy(6);
        if (DynamicVars.TryGetValue("Cards", out var cardsVar))
            cardsVar.UpgradeValueBy(1);
    }

    // 根据 reloads 和升级加成重新计算当前数值
    internal void RefreshDynamicVars()
    {
        var reloads = GetReloadCount();
        var finalDamage = Math.Max(3, 10 - reloads) + _upgradeDamageBonus;
        var finalDraw = Math.Max(1, 2 - reloads / 3) + _upgradeDrawBonus;

        if (DynamicVars.TryGetValue("Damage", out var dmgVar))
            dmgVar.BaseValue = finalDamage;
        if (DynamicVars.TryGetValue("Cards", out var cardsVar))
            cardsVar.BaseValue = finalDraw;
    }

    private static int GetReloadCount()
    {
        return FlybackManager.GetReloadCount();
    }
}