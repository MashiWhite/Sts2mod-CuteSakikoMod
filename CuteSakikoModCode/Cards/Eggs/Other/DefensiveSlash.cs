using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Other;

public class DefensiveSlash() : ModStatusCard(0, CardType.Status, CardRarity.Status, TargetType.None)
{
    private bool _effectTriggered;


    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

    protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

    // 抽到此牌时触发
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card != this) return;
        await ExecuteEffect(choiceContext);
    }

    // 公开方法，供外部直接调用（例如生成时立即触发）
    public async Task ExecuteEffect(PlayerChoiceContext choiceContext)
    {
        if (_effectTriggered) return;
        _effectTriggered = true;

        // 对随机敌人造成伤害
        var enemies = CombatState?.HittableEnemies;
        if (enemies != null && enemies.Any())
        {
            var target = Owner.RunState.Rng.CombatCardSelection.NextItem(enemies);
            var damage = IsUpgraded ? 3 : 2;
            await DamageCmd.Attack(damage)
                .FromCard(this)
                .Targeting(target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }

        // 获得格挡
        var block = IsUpgraded ? 20 : 14;
        await CreatureCmd.GainBlock(Owner.Creature, new BlockVar(block, ValueProp.Move), null);

        // 结束此回合
        PlayerCmd.EndTurn(Owner, false);

        // 移除这张防御斩（避免残留）
        await CardPileCmd.RemoveFromCombat(this);
    }

    protected override void OnUpgrade()
    {
        // 升级效果在逻辑中通过 IsUpgraded 处理
    }
}