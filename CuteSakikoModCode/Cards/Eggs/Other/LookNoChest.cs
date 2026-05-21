using CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Common;
using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Other;

public class LookNoChest() : ModTokenCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    // 虚无、消耗
    public override IEnumerable<CardKeyword> CanonicalKeywords =>  [CardKeyword.Ethereal, CardKeyword.Exhaust,CutesakiKeywords.Nochest.GetModKeywordCardKeyword() ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = Owner.Creature.CombatState;
        if (combatState == null) return;

        // 获取队友数量（排除自己）
        var allPlayers = combatState.Players.ToList();
        var teammates = allPlayers.Where(p => p != Owner).ToList();
        var teammateCount = teammates.Count;
        if (teammateCount == 0) return; // 无队友，无法减费

        int reduction;
        if (IsUpgraded)
            reduction = 12 / teammateCount; // 整数除法，向下取整
        else
            reduction = 12 / (teammateCount * 2);
        if (reduction <= 0) return;

        // 遍历所有玩家，减少他们持有的 NoChest 费用
        foreach (var player in combatState.Players)
        {
            var noChestCards = player.PlayerCombatState?.AllCards.OfType<NoChest>();
            if (noChestCards != null)
                foreach (var noChest in noChestCards)
                    noChest.ApplyReduction(reduction);
        }
    }

    protected override void OnUpgrade()
    {
    }
}