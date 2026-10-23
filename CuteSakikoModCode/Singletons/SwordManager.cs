using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Common;
using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Token;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Uncommon;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Singletons;

[RegisterSingleton]
public sealed class SwordManager : HookedSingletonModel
{
    public SwordManager() : base(HookType.Combat)
    {
    }

    private static bool IsSword(CardModel card, CardModel? exclude)
    {
        if (card == exclude) return false;
        return card is KnightSword;
    }

    public static async Task EnsureSwordExists(
        Player player,
        bool upgrade = false,
        CardModel? exclude = null,
        bool includeExhaustAndForget = false)
    {
        if (player?.Creature?.CombatState == null) return;

        foreach (var pileType in new[] { PileType.Hand, PileType.Draw, PileType.Discard })
        {
            var pile = pileType.GetPile(player);
            if (pile == null) continue;
            if (pile.Cards.Any(c => IsSword(c, exclude)))
                return;
        }

        if (includeExhaustAndForget)
        {
            var exhaust = PileType.Exhaust.GetPile(player);
            if (exhaust != null && exhaust.Cards.Any(c => IsSword(c, exclude)))
                return;

            var forgetPile = ForgetCardPile.Get(player);
            if (forgetPile != null && forgetPile.Cards.Any(c => IsSword(c, exclude)))
                return;
        }

        var sword = player.Creature.CombatState.CreateCard<KnightSword>(player);

        if (upgrade && sword.IsUpgradable)
        {
            sword.UpgradeInternal();
            sword.FinalizeUpgradeInternal();
        }

        await CardPileCmd.AddGeneratedCardToCombat(sword, PileType.Hand, player);
    }

    /// <summary>
    ///     增加指定玩家所有骑士之剑（手牌/抽牌堆/弃牌堆/消耗堆/遗忘堆）的伤害。
    /// </summary>
    public static void IncreasePlayerSwordDamage(Player player, int delta)
    {
        if (delta <= 0) return;
        if (player?.Creature?.CombatState == null) return;

        foreach (var pileType in new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust })
        {
            var pile = pileType.GetPile(player);
            if (pile == null) continue;
            foreach (var card in pile.Cards)
                if (card is KnightSword ks)
                    ks.DynamicVars.Damage.BaseValue += delta;
        }

        var forgetPile = ForgetCardPile.Get(player);
        if (forgetPile != null)
            foreach (var card in forgetPile.Cards)
                if (card is KnightSword ks)
                    ks.DynamicVars.Damage.BaseValue += delta;
    }

    /// <summary>
    ///     增加所有玩家所有骑士之剑的伤害。
    /// </summary>
    public static void IncreaseAllSwordDamage(int delta, ICombatState combatState)
    {
        if (delta <= 0 || combatState == null) return;
        foreach (var player in combatState.Players)
            IncreasePlayerSwordDamage(player, delta);
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        var playedCard = cardPlay.Card;
        if (playedCard == null) return Task.CompletedTask;

        var swordKeyword = CutesakiKeywords.Sword.GetModCardKeyword();
        if (!playedCard.Keywords.Contains(swordKeyword)) return Task.CompletedTask;

        var player = playedCard.Owner;
        if (player?.Creature?.CombatState == null) return Task.CompletedTask;

        var includeExhaustAndForget = playedCard is Unsheathe;

        return EnsureSwordExists(player, false, playedCard, includeExhaustAndForget);
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);

        if (power is not PressurePower) return;
        if (amount <= 0) return;

        var combatState = power.CombatState;
        if (combatState == null) return;

        var delta = (int)amount;
        if (delta <= 0) return;

        // 如果这个压力是某个玩家身上的，记录下来
        var pressureOwner = power.Owner;
        Player? pressurePlayer = null;
        if (pressureOwner != null && pressureOwner.IsPlayer)
            pressurePlayer = combatState.Players.FirstOrDefault(p => p.Creature == pressureOwner);

        foreach (var targetPlayer in combatState.Players)
        {
            var hasRelic = targetPlayer.Relics.OfType<OblivionisSword>().Any();

            if (hasRelic)
            {
                // 有遗物：任何人（玩家/敌人）压力增加，都为这个玩家的剑增伤
                IncreasePlayerSwordDamage(targetPlayer, delta);
                targetPlayer.Relics.OfType<OblivionisSword>().FirstOrDefault()?.Flash();
            }
            else if (pressurePlayer != null && targetPlayer == pressurePlayer)
            {
                // 无遗物：只有自己身上的压力增加时才增伤
                IncreasePlayerSwordDamage(targetPlayer, delta);
            }
        }
    }
}