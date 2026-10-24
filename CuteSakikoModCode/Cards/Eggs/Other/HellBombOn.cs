using CuteSakikoMod.CuteSakikoModCode.Cards.Mod;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Other;

public class HellBombOn() : OtherModTokenCard(1, CardType.Skill, CardRarity.Token, TargetType.AnyAlly)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    public override bool HasTurnEndInHandEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(75m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromKeyword(CardKeyword.Exhaust); }
    }

    // 主动打出：传递给目标队友
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;
        var targetPlayer = cardPlay.Target.Player;
        if (targetPlayer == null) return;

        // 创建新的炸弹给目标（保持升级状态）
        var newBomb = CombatState.CreateCard<HellBombOn>(targetPlayer);
        if (IsUpgraded) CardCmd.Upgrade(newBomb);
        await CardPileCmd.AddGeneratedCardToCombat(newBomb, PileType.Hand, Owner);

        // 移除当前炸弹
        await CardPileCmd.RemoveFromCombat(this);
    }

    // 回合结束在手牌中：爆炸（对敌人和自己同时造成伤害）
    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        var targets = new List<Creature>();
        
        // 加入所有可攻击的敌人
        if (CombatState.HittableEnemies != null)
            targets.AddRange(CombatState.HittableEnemies);
            
        // 加入自己
        if (Owner.Creature != null && Owner.Creature.IsAlive)
            targets.Add(Owner.Creature);

        if (targets.Count == 0) return;

        // 1. 播放火焰烟雾特效（参考 ExplosiveAmpoule）
        var room = NCombatRoom.Instance;
        if (room != null)
        {
            foreach (var target in targets)
            {
                room.CombatVfxContainer.AddChildSafely(NFireSmokePuffVfx.Create(target));
            }
        }
        await Cmd.CustomScaledWait(0.2f, 0.3f);

        // 2. 对目标和自身同时造成伤害
        await CreatureCmd.Damage(
            choiceContext,
            targets,
            DynamicVars.Damage.BaseValue,
            DynamicVars.Damage.Props,
            Owner.Creature,
            this, // cardSource 传入 this，确保能吃到相关的增伤/减伤效果
            null);

        // 3. 移除自身
        if (!CombatManager.Instance.IsEnding && Pile != null && Pile.IsCombatPile)
            await CardPileCmd.RemoveFromCombat(this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(25m); // 75 → 100
    }
}