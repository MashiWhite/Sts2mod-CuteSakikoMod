using System.Linq;
using System.Threading.Tasks;
using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Basic;
using CuteSakikoMod.CuteSakikoModCode.Enchantments;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Systems.Chord;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;   // SavedProperty / SerializationCondition
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Event;

public class EmoMask : CuteSakikoEventRelic, IChordNoteHookHandler
{
    private const int Threshold = 7;

    private int _counter;

    // 跨战斗持久化：用属性承载 SavedProperty
    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    private int Counter
    {
        get => _counter;
        set
        {
            _counter = value;
            // 只在 mutable 状态下刷新 UI，反序列化期间略过
            if (IsMutable)
                InvokeDisplayAmountChanged();
        }
    }

    public override RelicRarity Rarity => RelicRarity.Event;
    public override bool ShowCounter => true;
    public override int DisplayAmount => Counter;

    // ---------- 悬浮提示 ----------
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            // 压力能力
            yield return HoverTipFactory.FromPower<PressurePower>();
            // 演奏关键词
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Playpiano.GetModCardKeyword());
            // 演奏附魔
            foreach (var tip in HoverTipFactory.FromEnchantment<PlayEnchantment>())
                yield return tip;
        }
    }

    // ---------- 压力计数 ----------
    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (Owner == null) return;
        if (power is not PressurePower) return;
        if (amount <= 0) return;
        if (applier != Owner.Creature) return;

        await IncrementCounter(choiceContext);
    }

    // ---------- 演奏和弦计数 ----------
    public Task BeforeNoteAdded(Player player, CardType noteType, PlayerChoiceContext? context)
        => Task.CompletedTask;

    public Task AfterNoteAdded(Player player, CardType noteType, PlayerChoiceContext? context)
        => Task.CompletedTask;

    public Task BeforeChordMatched(Player player, string chordId, PlayerChoiceContext? context)
        => Task.CompletedTask;

    public Task AfterChordMatched(Player player, string chordId, PlayerChoiceContext? context)
        => Task.CompletedTask;

    public Task BeforeChordPlayed(Player player, string chordId, int bonus, PlayerChoiceContext? context)
        => Task.CompletedTask;

    public async Task AfterChordPlayed(Player player, string chordId, int bonus, PlayerChoiceContext? context)
    {
        if (Owner == null) return;
        if (player != Owner) return;
        await IncrementCounter(context);
    }

    // ---------- 计数与触发 ----------
    private async Task IncrementCounter(PlayerChoiceContext? context)
    {
        Counter += 1;
        Flash();

        if (Counter >= Threshold)
        {
            Counter = 0;
            if (context != null)
                await GiveRandomPianoCard(context);
        }
    }

    private async Task GiveRandomPianoCard(PlayerChoiceContext context)
    {
        if (Owner?.Creature.CombatState == null) return;

        var pianoKeyword = CutesakiKeywords.Playpiano.GetModCardKeyword();

        var pianoCandidates = ModelDb.AllCards
            .Where(c => c.CanonicalKeywords.Contains(pianoKeyword) &&
                        c.Id != ModelDb.Card<PianoStrike>().Id)
            .ToList();

        if (pianoCandidates.Count == 0) return;

        // 官方工厂：内部会把卡注册到 CombatState，并使用战斗专用 RNG 保证多人同步
        var cards = CardFactory.GetDistinctForCombat(
            Owner,
            pianoCandidates,
            1,
            Owner.RunState.Rng.CombatCardGeneration);

        var newCard = cards.FirstOrDefault();
        if (newCard == null) return;

        // 附魔：演奏
        var canonicalEnchantment = ModelDb.Enchantment<PlayEnchantment>();
        var mutableEnchantment = canonicalEnchantment.ToMutable();
        CardCmd.Enchant(mutableEnchantment, newCard, 1);

        // 免费
        newCard.SetToFreeThisTurn();

        // 加入手牌
        await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, Owner);
    }
}