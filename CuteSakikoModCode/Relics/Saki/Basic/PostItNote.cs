using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using CuteSakikoMod.CuteSakikoModCode.Singletons;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Basic;

public sealed class PostItNote : CuteSakikoModRelic
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

    public override async Task AfterSideTurnStart(CombatSide side, ICombatState combatState)
    {
        if (side != Owner.Creature.Side) return;

        if (combatState.RoundNumber == 1)
        {
            await PowerCmd.Apply<PressurePower>(new ThrowingPlayerChoiceContext(), Owner.Creature, 5, Owner.Creature,
                null);
            Flash();
        }

        if (combatState.HittableEnemies != null)
            foreach (var enemy in combatState.HittableEnemies)
                await PowerCmd.Apply<PressurePower>(new ThrowingPlayerChoiceContext(), enemy, 5, Owner.Creature, null);
    }

    /// <summary>
    ///     右键打开回忆卡牌查看界面（复用原版牌堆系统）
    /// </summary>
    public void OpenMemoryLibrary()
    {
        var player = Owner;
        if (player == null) return;

        var templates = ModelDb.AllCards
            .Where(c => c.HasModKeyword(CutesakiKeywords.Memory) &&
                        !SakiMemoryManager.Instance.ExhaustedMemoryIds.Contains(c.Id))
            .ToList();

        if (templates.Count == 0) return;

        var cards = templates.Select(t => player.RunState.CreateCard(t, player)).ToList();

        var pile = new CardPile(PileType.Exhaust);
        foreach (var card in cards)
            pile.AddInternal(card);

        NCardPileScreen.ShowScreen(pile, Array.Empty<string>());
    }
}