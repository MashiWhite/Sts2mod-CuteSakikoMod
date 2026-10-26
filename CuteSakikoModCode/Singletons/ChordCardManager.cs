using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CuteSakikoMod.CuteSakikoModCode.Cards.Anon;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using CuteSakikoMod.CuteSakikoModCode.Systems.Chord;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Singletons;

[RegisterSingleton]
public sealed class ChordCardManager : HookedSingletonModel
{
    public ChordCardManager() : base(HookType.Combat) { }

    // ★ 幂等保护：以 CombatState 为 key，记录本场战斗已处理的玩家
    // 用 ConditionalWeakTable：CombatState 被回收时条目自动消失，不会跨战斗累积
    private static readonly ConditionalWeakTable<ICombatState, HashSet<ulong>> _processed = new();

    public static async Task ProcessPlayerChordCardsAsync(Player player)
    {
        if (player == null) return;

        var combat = player.Creature?.CombatState;
        if (combat == null) return;

        var set = _processed.GetOrCreateValue(combat);
        if (!set.Add(player.NetId)) return; // 本场已处理，跳过

        var chords = player.GetChords();
        if (chords == null) return;

        var deck = PileType.Deck.GetPile(player);
        if (deck == null) return;

        var grouped = deck.Cards
            .Where(c => c.Keywords != null &&
                        c.Keywords.Contains(CutesakiKeywords.Chord.GetModCardKeyword()))
            .Select(c => new { card = c, chordId = (c as CuteAnonCard)?.ChordId })
            .Where(x => !string.IsNullOrEmpty(x.chordId) &&
                        ChordManager.AllChords.ContainsKey(x.chordId))
            .GroupBy(x => x.chordId)
            .ToList();

        chords.ClearChordCardBonus();

        if (grouped.Count == 0) return;

        var ctx = new HookPlayerChoiceContext(player, player.NetId, GameActionType.Combat);
        var chordsToPlay = new List<(string chordId, bool applyUpgraded)>();

        foreach (var group in grouped)
        {
            var chordId = group.Key;
            var cards = group.Select(x => x.card).ToList();

            // 1) 加临时槽位
            if (!chords.GetTemporaryChords().Contains(chordId))
                chords.AddTemporaryChord(chordId);

            // 2) 只要有任何升级卡，ChordCardBonus +1
            if (cards.Any(c => c.IsUpgraded))
                chords.AddChordCardBonus(chordId, 1);

            // 3) 排序：升级卡在前，未升级卡在后
            cards.Sort((a, b) => (b.IsUpgraded ? 1 : 0) - (a.IsUpgraded ? 1 : 0));

            // 4) 第一张只给槽位，第二张起触发演奏
            for (int i = 1; i < cards.Count; i++)
            {
                var card = cards[i];
                chordsToPlay.Add((chordId, card.IsUpgraded));
            }
        }

        if (chordsToPlay.Count > 0)
        {
            var task = ChordNoteSystem.PlayChordsAsync(player, chordsToPlay, ctx);
            await ctx.AssignTaskAndWaitForPauseOrCompletion(task);
        }
    }

    // ==================== 洗牌时排除 Chord 卡 ====================
    public override void ModifyShuffleOrder(
        Player player, List<CardModel> cards, bool isInitialShuffle)
    {
        if (!isInitialShuffle) return;
        if (player.GetChords() == null) return;

        cards.RemoveAll(c => c.Keywords != null &&
                             c.Keywords.Contains(CutesakiKeywords.Chord.GetModCardKeyword()));
    }
}