
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare
{
    public class PlayTogether() : CuteAnonCard(-1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        protected override bool HasEnergyCostX => true;

        public override IEnumerable<CardKeyword> CanonicalKeywords
        {
            get { yield return CardKeyword.Exhaust; }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            TriggerBanter();

            var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
            if (guitar == null) return;

            var chordIds = guitar.GetLearnedChordIds(); // 所有已学习和弦
            if (chordIds.Count == 0) return;

            int x = ResolveEnergyXValue();
            int times = IsUpgraded ? x + 2 : x; // 升级后 X+2 次
            if (times <= 0) return;

            var rng = Owner.RunState.Rng.CombatCardSelection;
            var creature = Owner.Creature;
            int multiplier = guitar.GetEffectMultiplier(); // 使用新增的公共方法

            for (int i = 0; i < times; i++)
            {
                string randomChordId = rng.NextItem(chordIds);
                if (ChordManager.AllChords.TryGetValue(randomChordId, out var def))
                {
                    await def.Effect(choiceContext, creature, multiplier);
                }
            }
        }

        protected override void OnUpgrade()
        {
            // 效果本身由 IsUpgraded 控制次数
        }
    }
}