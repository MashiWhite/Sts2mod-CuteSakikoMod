
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Uncommon;

public class LinBingDouZhe() : CuteSakikoModEggCard(
    0,
    CardType.Skill,
    CardRarity.Uncommon,
    TargetType.AllEnemies)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 获取所有存活敌人
        var enemies = CombatState.Enemies
            .Where(c => c.IsAlive && c.IsMonster)
            .ToList();

        foreach (var enemy in enemies)
        {
            var monster = enemy.Monster;
            if (monster == null)
                continue;

            // 获取安全的后续状态 ID，避免状态机断裂
            string? safeFollowUpId = MonsterMoveHelper.GetSafeFollowUpId(monster);
            if (safeFollowUpId == null)
                continue;

            var escapeIntent = new EscapeIntent();

            // 注意：闭包捕获 enemy，每个敌人独立创建 MoveState
            var escapeMove = new MoveState(
                "LIN_BING_DOU_ZHE_ESCAPE",
                async _ =>
                {
                    if (enemy.IsAlive)
                        await CreatureCmd.Escape(enemy);
                },
                escapeIntent)
            {
                FollowUpStateId = safeFollowUpId
            };

            if (enemy.IsAlive && enemy.Monster != null)
                monster.SetMoveImmediate(escapeMove, true);
        }

        // 永久删除牌库中的这张卡牌（整局游戏移除）
        if (DeckVersion != null)
            await CardPileCmd.RemoveFromDeck(DeckVersion);

        // 从战斗中移除当前实例
        await CardPileCmd.RemoveFromCombat(this);
    }

    protected override void OnUpgrade()
    {
        // 升级后获得“虚无”
        AddKeyword(CardKeyword.Ethereal);
    }
}