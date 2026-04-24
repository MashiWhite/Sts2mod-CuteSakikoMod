
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon
{
    public class TgwGroup() : CuteAnonCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        /// <summary>嵌套动态变量，实时计算总金币</summary>
        private class TgwGoldVar : DynamicVar
        {
            public TgwGoldVar() : base("TgwGold", 0m) { }

            public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
            {
                base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);

                int perGold = (int)card.DynamicVars["BaseGold"].BaseValue;
                var combat = card.CombatState;
                if (combat != null)
                {
                    int aliveEnemies = combat.Enemies.Count(e => e.IsAlive);
                    int alivePlayers = combat.Players.Select(p => p.Creature).Count(c => c.IsAlive);
                    // 实时更新 BaseValue，UI 读取时自动使用最新值
                    BaseValue = perGold * (aliveEnemies + alivePlayers);
                }
                else
                {
                    // 非战斗场景设为0，描述会走 {InCombat:...|...} 的第二个分支
                    BaseValue = 0;
                }
            }
        }

        protected override IEnumerable<DynamicVar> CanonicalVars
        {
            get
            {
                yield return new DynamicVar("BaseGold", 10m); // 基础倍数
                yield return new TgwGoldVar();                 // 实时总金币
            }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            TriggerBanter();

            // 打出时使用与预览相同的计算方式
            int perGold = (int)DynamicVars["BaseGold"].BaseValue;
            var combat = CombatState;
            if (combat == null) return;

            int aliveEnemies = combat.Enemies.Count(e => e.IsAlive);
            int alivePlayers = combat.Players.Select(p => p.Creature).Count(c => c.IsAlive);
            int totalGold = perGold * (aliveEnemies + alivePlayers);

            if (totalGold > 0)
                await PlayerCmd.GainGold(totalGold, Owner);
        }

        protected override void OnUpgrade()
        {
            DynamicVars["BaseGold"].UpgradeValueBy(5m); // 10 → 15
        }
    }
}