using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using CuteSakikoMod.CuteSakikoModCode.Relics.Rana.Starter;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public class WantBothPower : CuteSakikoModPower
{
    private MatchaParfait? _subscribedParfait;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    // 能力被施加时订阅遗物事件
    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);
        if (_subscribedParfait != null) return;

        var parfait = Owner.Player?.GetRelic<MatchaParfait>();
        if (parfait == null) return;

        _subscribedParfait = parfait;
        parfait.ChargesRemoved += OnParfaitConsumed;
    }

    // 能力被移除时取消订阅
    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (_subscribedParfait != null)
        {
            _subscribedParfait.ChargesRemoved -= OnParfaitConsumed;
            _subscribedParfait = null;
        }
        await base.AfterRemoved(oldOwner);
    }

    private async void OnParfaitConsumed(Player player, int amount, PlayerChoiceContext? choiceContext)
    {
        if (player != Owner.Player) return;
        if (Amount <= 0) return;
        if (choiceContext == null) return;

        // 优先检查能力是否还存在（最快速失败）
        if (!Owner.Player.Creature.HasPower<WantBothPower>()) return;

        if (CombatManager.Instance.IsOverOrEnding) return;
        if (Owner.Player?.Creature?.CombatState == null) return;

        for (int i = 0; i < amount; i++)
        {
            await PlayerCmd.GainEnergy(1, player);
            await CardPileCmd.Draw(choiceContext, 1, player);
        }
    }
}