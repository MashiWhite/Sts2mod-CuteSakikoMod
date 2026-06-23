using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.Character.Mujica;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Starter;

[RegisterCharacterStarterRelic(typeof(CuteSaki))]
[RegisterTouchOfOrobasRefinement(typeof(PostItNote))]
public class KabutoNote : CuteSakiRelic
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

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != Owner.Creature.Side || combatState.RoundNumber != 1)
            return;

        // 1. 开局给压力（原有效果）
        int powerCount = Math.Min(3, Owner.Creature.CurrentHp - 1);
        await PowerCmd.Apply<PressurePower>(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature, powerCount, Owner.Creature, null);

        // 2. 确保记忆牌堆已初始化（防止读档时牌堆为空）
        await MemoryCardPile.EnsureInitializedAsync(Owner);

        // 3. 随机获取一张回忆卡牌（不升级）
        await MemoryCmd.Recall(new ThrowingPlayerChoiceContext(), Owner, allowChoose: false, count: 1, upgraded: false, source: null);

        Flash();
    }
}