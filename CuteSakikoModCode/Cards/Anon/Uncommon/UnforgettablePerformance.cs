
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon
{
    public class UnforgettablePerformance() : CuteAnonCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        public override IEnumerable<CardKeyword> CanonicalKeywords
        {
            get
            {
                yield return CutesakiKeywords.NoNote; // 自身不产生音符
            }
        }
        
        protected override IEnumerable<IHoverTip> ExtraHoverTips
        {
            get
            {
                yield return HoverTipFactory.FromPower<UnforgettablePerformancePower>();
            }
        }

        protected override IEnumerable<DynamicVar> CanonicalVars
        {
            get
            {
                // 能力层数固定为 1 层
                yield return new PowerVar<UnforgettablePerformancePower>(1m);
                // 描述中使用的能量数值变量
                yield return new EnergyVar(1);
            }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            TriggerBanter();

            // 给予 1 层能力
            await PowerCmd.Apply<UnforgettablePerformancePower>(Owner.Creature, 1, Owner.Creature, this);
        }

        protected override void OnUpgrade()
        {
            // 能力层数不变（仍为 1 层），但描述中的能量数值升级
            DynamicVars.Energy.UpgradeValueBy(1); // 1 → 2
        }
    }
}