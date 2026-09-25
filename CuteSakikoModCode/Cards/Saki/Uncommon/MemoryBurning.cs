using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

public class MemoryBurning : CuteSakikoModCard
{
    public MemoryBurning() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Memory.GetModCardKeyword());
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Sakiforget.GetModCardKeyword());
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        var forgetPile = ForgetCardPile.Get(Owner);
        if (forgetPile == null || forgetPile.Cards.Count == 0) return;

        var memoryKeyword = CutesakiKeywords.Memory.GetModCardKeyword();
        
        var memoryCards = forgetPile.Cards
            .Where(c => c.Keywords.Contains(memoryKeyword))
            .ToList();

        if (memoryCards.Count == 0) return;

        var target = GetRandomEnemy();

        foreach (var card in memoryCards)
        {
            await CardCmd.AutoPlay(choiceContext, card, null);

            await CardCmd.Exhaust(choiceContext, card);
        }

        forgetPile.InvokeContentsChanged();
    }

    private Creature? GetRandomEnemy()
    {
        var enemies = CombatState?.HittableEnemies;
        if (enemies == null || enemies.Count == 0) return null;

        // ★ 先排序再随机索引，保证两端随机池一致
        var orderedEnemies = enemies
            .OrderBy(e => e.CombatId ?? uint.MaxValue)
            .ToList();

        return orderedEnemies[Owner.RunState.Rng.CombatCardSelection.NextInt(orderedEnemies.Count)];
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}