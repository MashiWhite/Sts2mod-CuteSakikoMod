
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon
{
    public class SongOfStrength() : CuteAnonCard(-1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        protected override bool HasEnergyCostX => true;
        
        protected override IEnumerable<IHoverTip> ExtraHoverTips
        {
            get
            {
                yield return HoverTipFactory.FromPower<StrengthPower>();
                yield return HoverTipFactory.FromPower<DexterityPower>();
            }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            TriggerBanter();

            var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
            if (guitar == null) return;

            var chordIds = guitar.GetLearnedChordIds();
            if (chordIds.Count == 0) return;

            int attackCount = 0;
            int skillCount = 0;

            foreach (var chordId in chordIds)
            {
                if (ChordManager.AllChords.TryGetValue(chordId, out var def))
                {
                    foreach (var type in def.NoteSequence)
                    {
                        if (type == CardType.Attack) attackCount++;
                        else if (type == CardType.Skill) skillCount++;
                    }
                }
            }

            int x = ResolveEnergyXValue();
            if (x <= 0) return;

            int perN = IsUpgraded ? 2 : 3;

            int strengthAmount = (attackCount / perN) * x;
            int dexterityAmount = (skillCount / perN) * x;

            if (strengthAmount > 0)
                await PowerCmd.Apply<StrengthPower>(choiceContext,Owner.Creature, strengthAmount, Owner.Creature, this);
            if (dexterityAmount > 0)
                await PowerCmd.Apply<DexterityPower>(choiceContext,Owner.Creature, dexterityAmount, Owner.Creature, this);
        }

        protected override void OnUpgrade()
        {
            // 效果已在IsUpgraded中处理
        }
    }
}