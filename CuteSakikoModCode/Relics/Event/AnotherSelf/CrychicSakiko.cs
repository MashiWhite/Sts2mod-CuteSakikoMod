using CuteSakikoMod.CuteSakikoModCode.Map;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Event.AnotherSelf;

public class CrychicSakiko : CuteSakikoEventRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override async Task AfterObtained()
    {
        // 1组普通、1组罕见、2组稀有
        var rewards = new List<Reward>
        {
            CreateCardReward(CardRarity.Common),
            CreateCardReward(CardRarity.Uncommon),
            CreateCardReward(CardRarity.Rare),
            CreateCardReward(CardRarity.Rare)
        };
        await RewardsCmd.OfferCustom(Owner, rewards);

        if (Owner.RunState.CurrentActIndex == 1)
            await RunManager.Instance.GenerateMap();
    }

    private CardReward CreateCardReward(CardRarity rarity)
    {
        var options = CardCreationOptions.ForNonCombatWithUniformOdds(
                new[] { Owner.Character.CardPool },
                c => c.Rarity == rarity)
            .WithFlags(CardCreationFlags.NoRarityModification);
        return new CardReward(options, 3, Owner);
    }

    public override ActMap ModifyGeneratedMap(IRunState runState, ActMap map, int actIndex)
    {
        if (actIndex != 1)
            return map;

        // 如果地图已经被本模组的缩放地图接管，不再重复缩放
        if (map is ScaledActMap)
            return map;

        return new ScaledActMap((RunState)runState, map, 2.0);  // 或 0.5
    }
}