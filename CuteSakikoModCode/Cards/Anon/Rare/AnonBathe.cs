using CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Other;
using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare;

public class AnonBathe() : CuteAnonCard(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    private static bool _isTransforming;

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        var debuffs = Owner.Creature.Powers.Where(p => p.Type == PowerType.Debuff).ToList();
        if (debuffs.Any())
        {
            var toRemove = debuffs[Owner.RunState.Rng.CombatCardSelection.NextInt(debuffs.Count)];
            await PowerCmd.Remove(toRemove);
        }

        await Cmd.CustomScaledWait(0.1f, 0.15f);
        await TryCombine();
    }

    private async Task TryCombine()
    {
        // 使用类型作为锁对象，确保全局互斥
        var lockObj = typeof(AnonBathe);
        var lockTaken = false;
        try
        {
            Monitor.Enter(lockObj, ref lockTaken);
            if (_isTransforming) return;
            _isTransforming = true;
        }
        finally
        {
            if (lockTaken) Monitor.Exit(lockObj);
        }

        try
        {
            if (CombatState == null || Owner == null) return;

            var hand = PileType.Hand.GetPile(Owner);
            if (hand == null) return;

            var sakiCard = hand.Cards.OfType<SakiBathe>().FirstOrDefault();
            if (sakiCard == null) return;

            var shouldUpgrade = IsUpgraded || sakiCard.IsUpgraded;

            // 修复点1：添加第二个参数 false
            if (sakiCard.Pile != null && sakiCard.Pile.IsCombatPile && sakiCard.Pile.Type != PileType.Play &&
                sakiCard.Pile.Type != PileType.Exhaust)
                await CardPileCmd.RemoveFromCombat(sakiCard);

            // 修复点2：添加第二个参数 false
            if (Pile != null && Pile.IsCombatPile && Pile.Type != PileType.Play && Pile.Type != PileType.Exhaust)
                await CardPileCmd.RemoveFromCombat(this);

            var combined = CombatState.CreateCard<AnonSakiBathe>(Owner);
            if (shouldUpgrade)
            {
                combined.UpgradeInternal();
                combined.FinalizeUpgradeInternal();
            }

            await CardPileCmd.AddGeneratedCardToCombat(combined, PileType.Hand, Owner);
        }
        finally
        {
            _isTransforming = false;
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}