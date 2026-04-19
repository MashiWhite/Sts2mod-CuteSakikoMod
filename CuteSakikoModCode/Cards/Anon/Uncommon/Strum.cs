using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Token;
using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon
{
    public class Strum : CuteAnonCard
    {
        public Strum() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
        {
        }

        public override IEnumerable<CardKeyword> CanonicalKeywords
        {
            get
            {
                yield return CardKeyword.Exhaust;
                yield return CutesakiKeywords.NoNote; // 自身不产生音符
            }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            TriggerBanter();
            var combatState = Owner.Creature.CombatState;
            if (combatState == null) return;

            // 准备三种音符的模板（规范实例）
            var templateAtk = ModelDb.Card<AtkNote>();
            var templateSkill = ModelDb.Card<SkillNote>();
            var templatePower = ModelDb.Card<PowerNote>();

            // 连续选择三次
            for (int i = 0; i < 3; i++)
            {
                // 每次选择前从模板克隆全新实例，并关联战斗状态
                var options = new List<CardModel>
                {
                    combatState.CreateCard<AtkNote>(Owner),
                    combatState.CreateCard<SkillNote>(Owner),
                    combatState.CreateCard<PowerNote>(Owner)
                };

                var selected = await CardSelectCmd.FromChooseACardScreen(
                    choiceContext,
                    options,
                    Owner,
                    canSkip: false
                );

                if (selected != null)
                {
                    await CardCmd.AutoPlay(choiceContext, selected, null);
                }
            }
        }

        protected override void OnUpgrade()
        {
            EnergyCost.UpgradeBy(-1);
        }
    }
}