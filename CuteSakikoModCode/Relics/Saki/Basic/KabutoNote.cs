using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.Character.Mujica;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Basic;

[RegisterCharacterStarterRelic(typeof(CuteSaki))]
[RegisterTouchOfOrobasRefinement(typeof(PostItNote))]
public class KabutoNote : CuteSakiRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<string> RegisteredKeywordIds => [CutesakiKeywords.Memorysaki];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != Owner.Creature.Side || combatState.RoundNumber != 1)
            return;

        // 1. 开局给压力（原有效果）
        await PowerCmd.Apply<PressurePower>(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature, 3, Owner.Creature, null);

        // 2. 确保记忆牌堆已初始化（防止读档时牌堆为空）
        await MemoryCardPile.EnsureInitializedAsync(Owner);

        // 3. 随机获取一张回忆卡牌
        var canonicalCards = MemoryCardPile.GetCanonicalCards(Owner);
        if (canonicalCards.Count == 0) return;

        var shuffled = canonicalCards.UnstableShuffle(Owner.RunState.Rng.Shuffle);
        var template = shuffled.FirstOrDefault();
        if (template == null) return;

        var mutableCard = Owner.Creature.CombatState.CreateCard(template, Owner);
        await CardPileCmd.AddGeneratedCardToCombat(mutableCard, PileType.Hand, Owner);

        Flash();
    }
}