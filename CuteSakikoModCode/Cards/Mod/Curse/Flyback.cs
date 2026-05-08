using System.Reflection;
using CuteSakikoMod.CuteSakikoModCode.Singletons;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Mod.Curse;

public class Flyback : ModTokenCard
{
    // 升级提升的数值（在 OnUpgrade 中赋值）
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
            int baseDamage = Math.Max(1, 8 - reloads);
            int baseDraw = Math.Max(1, 2 - (reloads / 3));

            // 加上升级提升的数值
            baseDamage += _upgradeDamageBonus;
            baseDraw += _upgradeDrawBonus;

            yield return new DamageVar(baseDamage, ValueProp.Move);
            yield return new CardsVar(baseDraw);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
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
        // 设置升级数值（例如伤害+3，抽牌+1，可以根据需求调整）
        _upgradeDamageBonus = 3;
        _upgradeDrawBonus = 1;
        // 如果需要，可以在这里更新动态变量，但文本描述会自动读取新的值
    }

    private static int GetReloadCount()
    {
        var field = typeof(RunManager).GetField("_numReloads", BindingFlags.NonPublic | BindingFlags.Instance);
        return field != null ? (int)field.GetValue(RunManager.Instance) : 0;
    }
}