using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Token;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Singletons;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Rare;

public class ByMyReign() : CuteSakikoModCard(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromCard<KnightSword>();
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Sword.GetModCardKeyword());
        }
    }

    protected override bool ShouldGlowGoldInternal
    {
        get
        {
            if (CombatState == null) return false;
            foreach (var player in CombatState.Players)
            {
                var hand = PileType.Hand.GetPile(player);
                if (hand != null && hand.Cards.Any(c => c is KnightSword))
                    return true;
            }

            return false;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (var player in CombatState.Players)
        {
            var hand = PileType.Hand.GetPile(player);
            if (hand == null) continue;

            var swords = hand.Cards.Where(c => c is KnightSword).ToList();

            if (swords.Count > 0)
            {
                foreach (var sword in swords)
                {
                    var target = GetRandomEnemy();
                    if (target == null) continue;

                    if (IsUpgraded) sword.BaseReplayCount += 1;

                    await CardCmd.AutoPlay(choiceContext, sword, target);

                    if (IsUpgraded) sword.BaseReplayCount -= 1;
                }
            }
            else
            {
                // 为所有人出鞘：若手牌/抽牌堆/弃牌堆中没有骑士之剑，补一把
                await SwordManager.EnsureSwordExists(player);
            }
        }
    }

    private Creature? GetRandomEnemy()
    {
        var enemies = CombatState?.HittableEnemies;
        if (enemies == null || enemies.Count == 0) return null;
        return enemies[Owner.RunState.Rng.CombatCardSelection.NextInt(enemies.Count)];
    }

    protected override void OnUpgrade()
    {
    }
}