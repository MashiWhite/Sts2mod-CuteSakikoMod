using CuteSakikoMod.CuteSakikoModCode.Character;
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
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Basic;

[RegisterCharacterStarterRelic(typeof(CuteSaki))]
[RegisterTouchOfOrobasRefinement(typeof(PostItNote))]
public class KabutoNote : CuteSakikoModRelic  // ← 去掉 sealed
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
        if (side == Owner.Creature.Side && combatState.RoundNumber == 1)
        {
            await PowerCmd.Apply<PressurePower>(new ThrowingPlayerChoiceContext(), Owner.Creature, 3, Owner.Creature, null);
            Flash();
        }
    }

    /// <summary>
    ///     右键打开回忆卡牌查看界面（复用原版牌堆查看系统）
    /// </summary>
    public void OpenMemoryLibrary()
    {
        var player = Owner;
        if (player == null) return;

        // 获取所有未消耗的回忆卡牌模板
        var templates = ModelDb.AllCards
            .Where(c => c.HasModKeyword(CutesakiKeywords.Memory) &&
                        !SakiMemoryManager.Instance.ExhaustedMemoryIds.Contains(c.Id))
            .ToList();

        if (templates.Count == 0) return;

        // 为每张卡牌创建可变实例（用于界面展示，需要 Owner 但非必要）
        var cards = templates.Select(t => player.RunState.CreateCard(t, player)).ToList();

        // 构造一个临时牌堆（类型随意，Exhaust 仅用于界面底部文字）
        var pile = new CardPile(PileType.Exhaust);
        foreach (var card in cards)
            pile.AddInternal(card);

        // 调用原版牌堆查看界面（关闭热键设为空数组）
        NCardPileScreen.ShowScreen(pile, Array.Empty<string>());
    }
}