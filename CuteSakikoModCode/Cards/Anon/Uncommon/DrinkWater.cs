
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon
{
    public class DrinkWater() : CuteAnonCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        protected override IEnumerable<DynamicVar> CanonicalVars
        {
            get
            {
                yield return new EnergyVar(1);
            }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            TriggerBanter();

            // 清除所有音符，并获取清除的数量
            int clearedCount = MusicNoteManager.ClearNotesAndGetCount(Owner);

            // 按清除数量抽牌
            if (clearedCount > 0)
                await CardPileCmd.Draw(choiceContext, clearedCount, Owner);

            // 获得能量（未升级1，升级后2）
            int energyGain = DynamicVars.Energy.IntValue;
            await PlayerCmd.GainEnergy(energyGain, Owner);

            // 更新音符UI（如果有吉他遗物）
            var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
            guitar?.UpdateNoteDisplay();
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Energy.UpgradeValueBy(1m); // 1 -> 2
        }
    }
}