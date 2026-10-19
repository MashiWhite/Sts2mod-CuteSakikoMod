using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx; 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Helpers;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class RainbowWorldPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    private readonly HashSet<ulong> _triggeredThisTurn = new();

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var original = cardPlay.Card;
        if (original == null || cardPlay.Player == null) return;
        if (!_triggeredThisTurn.Add(cardPlay.Player.NetId)) return;

        var combatState = Owner.CombatState;
        if (combatState == null) return;

        if (cardPlay.ResultPile == PileType.None) return;
        if (!original.IsTransformable) return;
        if (original.Pile?.Type != PileType.Play) return;

        var replacement = PickReplacement(original, cardPlay.Player);
        if (replacement == null) return;

        // 1. 将新卡静默加入目标牌堆（不播放动画）
        await CardPileCmd.Add(replacement, cardPlay.ResultPile, skipVisuals: true);
        
        // ★ 2. 手动刷新 UI，通知弃牌堆有新卡加入（解决不刷新问题）
        if (replacement.Pile != null)
        {
            replacement.Pile.InvokeCardAddFinished();
        }

        // 3. 获取原卡位置并销毁原卡节点
        var originalNode = NCard.FindOnTable(original);
        Vector2 originalPos = Vector2.Zero;
        if (originalNode != null)
        {
            originalPos = originalNode.GlobalPosition;
            originalNode.QueueFreeSafely();
        }

        // 4. 静默移除原卡数据
        original.RemoveFromCurrentPile(silent: true);

        // 5. 生成原生变牌特效，并修正坐标
        var vfx = NCardTransformVfx.Create(original, replacement, Array.Empty<RelicModel>());
        if (vfx != null)
        {
            var parent = NCombatRoom.Instance?.CombatVfxContainer;
            if (parent != null)
            {
                parent.AddChildSafely(vfx);
                // ★ 6. 将特效的坐标设为原卡牌在屏幕上的坐标（解决左上角问题）
                if (originalNode != null) 
                {
                    vfx.GlobalPosition = originalPos;
                }
            }
        }
    }

    public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Player) _triggeredThisTurn.Clear();
        return Task.CompletedTask;
    }

    private CardModel? PickReplacement(CardModel original, Player player)
    {
        var combatState = Owner.CombatState;
        if (combatState == null) return null;

        var myPool = player.Character.CardPool;
        var candidates = ModelDb.AllCards
            .Where(c => c.Rarity == original.Rarity
                     && c.Type == original.Type
                     && c.Pool != null
                     && c.Pool != myPool)
            .ToList();

        if (candidates.Count == 0) return null;

        var rng = combatState.RunState.Rng.CombatCardGeneration;
        var picked = candidates[rng.NextInt(candidates.Count)];
        return combatState.CreateCard(picked, player);
    }
}