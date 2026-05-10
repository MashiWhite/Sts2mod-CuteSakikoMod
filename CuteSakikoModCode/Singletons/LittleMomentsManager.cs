using System.Linq;
using System.Threading.Tasks;
using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare;
using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Token;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Singletons;

[RegisterSingleton]
public class LittleMomentsManager : HookedSingletonModel
{
    public LittleMomentsManager() : base(true, false)
    {
    }

    private bool _isSynthesizing;

    public override async Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel? source)
    {
        // 仅当 LittleMoments 进入手牌时处理
        if (card is not LittleMoments || card.Pile?.Type != PileType.Hand)
            return;

        if (_isSynthesizing || card.Owner == null) return;

        var hand = PileType.Hand.GetPile(card.Owner);
        if (hand == null) return;

        var littleMoments = hand.Cards.OfType<LittleMoments>().ToList();
        if (littleMoments.Count < 5) return;

        _isSynthesizing = true;
        try
        {
            // ★ 移除手牌中所有的 LittleMoments，而不是只移5张
            bool anyUpgraded = littleMoments.Any(c => c.IsUpgraded);

            foreach (var c in littleMoments)
                await CardPileCmd.RemoveFromCombat(c);

            // 通过 CombatState 创建卡牌，解决“户口”问题
            var combatState = card.Owner.Creature.CombatState;
            var lifetime = combatState.CreateCard<Lifetime>(card.Owner);

            if (anyUpgraded)
            {
                lifetime.UpgradeInternal();
                lifetime.FinalizeUpgradeInternal();
            }

            // 使用官方专用方法加入到手牌
            await CardPileCmd.AddGeneratedCardToCombat(lifetime, PileType.Hand, card.Owner);
        }
        finally
        {
            _isSynthesizing = false;
        }
    }
}