
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare
{
    public class ImpromptuPlay() : CuteAnonCard(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        public override IEnumerable<CardKeyword> CanonicalKeywords
        {
            get
            {
                yield return CardKeyword.Exhaust;
                yield return CutesakiKeywords.NoNote;
                yield return CutesakiKeywords.Chord;
            }
        }
        
        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            TriggerBanter();

            var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
            int multiplier = guitar?.GetEffectMultiplier() ?? 1;

            // 收集手牌、抽牌堆、弃牌堆（不包括消耗堆）
            var allPiles = new[]
            {
                PileType.Hand.GetPile(Owner),
                PileType.Draw.GetPile(Owner),
                PileType.Discard.GetPile(Owner)
            };

            var chordCards = allPiles
                .Where(p => p != null)
                .SelectMany(p => p.Cards)
                .Where(c => c.CanonicalKeywords.Contains(CutesakiKeywords.Chord))
                .ToList();

            foreach (var card in chordCards)
            {
                // 从卡牌自身获取和弦ID（要求卡牌重写了 ChordId）
                string chordId = (card as CuteAnonCard)?.ChordId;
                if (!string.IsNullOrEmpty(chordId) && ChordManager.AllChords.TryGetValue(chordId, out var def))
                {
                    // 先消耗，再演奏
                    await CardCmd.Exhaust(choiceContext, card);
                    await def.Effect(choiceContext, Owner.Creature, multiplier);
                }
            }
        }

        protected override void OnUpgrade()
        {
            EnergyCost.UpgradeBy(-1);   // 3 → 2
            AddKeyword(CardKeyword.Innate);
        }
    }
}