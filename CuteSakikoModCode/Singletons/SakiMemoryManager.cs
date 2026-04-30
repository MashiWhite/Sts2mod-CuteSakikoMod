using CuteSakikoMod.CuteSakikoModCode.Cards.Saki;
using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Singletons;

[RegisterSingleton]
public class SakiMemoryManager : SingletonModel
{
    private readonly HashSet<ModelId> _exhaustedMemoryIds = new();

    public SakiMemoryManager()
    {
        _exhaustedMemoryIds.Clear();
        
        // 订阅战斗事件钩子，这样 AfterCardPlayed、AfterCardExhausted 等方法才会被触发
        ModHelper.SubscribeForCombatStateHooks(Id.Entry, state => [this]);
    }

    public override bool ShouldReceiveCombatHooks => true;

    // 正确的获取实例方式
    public static SakiMemoryManager Instance => ModelDb.Singleton<SakiMemoryManager>();

    public IReadOnlyCollection<ModelId> ExhaustedMemoryIds => _exhaustedMemoryIds;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.HasModKeyword(CutesakiKeywords.Memory))
            cardPlay.Card.EnergyCost.UpgradeBy(1);
        await Task.CompletedTask;
    }

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card,
        bool causedByEthereal)
    {
        if (card.HasModKeyword(CutesakiKeywords.Memory))
        {
            if (card is SakiMemoryCard customCard)
            {
                await customCard.ProcessMemoryEffect(choiceContext);
                await customCard.ProcessMemoryEffect(choiceContext);
            }

            _exhaustedMemoryIds.Add(card.Id);
        }
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        _exhaustedMemoryIds.Clear();
        await Task.CompletedTask;
    }
}