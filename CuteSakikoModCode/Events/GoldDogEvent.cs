using CuteSakikoMod.CuteSakikoModCode.Encounters.Event;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace CuteSakikoMod.CuteSakikoModCode.Events;

[RegisterSharedEvent]
public sealed class GoldDogEvent : CuteSakikoEvent 
{
    public override bool IsShared => true;


    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://CuteSakikoMod/images/events/gold_dog.png");
  

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new GoldVar(20)
    };

    protected override bool IsAllowedInternal(IRunState runState) => runState.CurrentActIndex == 2;

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new List<EventOption>
        {
            new(this, FollowDog, InitialOptionKey("FOLLOW_DOG")),
            new(this, LeaveAndFight, InitialOptionKey("LEAVE_AND_FIGHT"))
        };
    }

    private async Task FollowDog()
    {
        await PlayerCmd.GainGold(DynamicVars.Gold.BaseValue, Owner!);
        SetEventFinished(PageDescription("DOG_TREASURE"));
    }

    private Task LeaveAndFight()
    {
        EnterCombatWithoutExitingEvent<Act1DoubleBossEncounter>(
            Array.Empty<Reward>(),
            shouldResumeAfterCombat: true
        );
        return Task.CompletedTask;
    }

    public override async Task Resume(AbstractRoom room)
    {
        // ★ 合并角色遗物池 + 共享遗物池中所有稀有遗物
        var characterRelics = Owner!.Character.RelicPool
            .GetUnlockedRelics(Owner.UnlockState)
            .Where(r => r.Rarity == RelicRarity.Rare);

        var sharedRelics = ModelDb.RelicPool<SharedRelicPool>()
            .GetUnlockedRelics(Owner.UnlockState)
            .Where(r => r.Rarity == RelicRarity.Rare);

        var rareRelics = characterRelics.Concat(sharedRelics)
            .Distinct()
            .ToList();

        if (rareRelics.Count > 0)
        {
            var chosen = Owner.PlayerRng.Rewards.NextItem(rareRelics).ToMutable();
            await RelicCmd.Obtain(chosen, Owner!);
        }
        
        // 两次稀有牌三选一
        var cardOptions1 = CardCreationOptions.ForNonCombatWithUniformOdds(
                new[] { Owner!.Character.CardPool },
                c => c.Rarity == CardRarity.Rare)
            .WithFlags(CardCreationFlags.NoRarityModification);
        var cardOptions2 = CardCreationOptions.ForNonCombatWithUniformOdds(
                new[] { Owner!.Character.CardPool },
                c => c.Rarity == CardRarity.Rare)
            .WithFlags(CardCreationFlags.NoRarityModification);

        await RewardsCmd.OfferCustom(Owner!, new List<Reward>
        {
            new CardReward(cardOptions1, 3, Owner),
            new CardReward(cardOptions2, 3, Owner)
        });
        

        SetEventFinished(PageDescription("DOG_TRAP"));
    }
}