
using System.Reflection;
using CuteSakikoMod.CuteSakikoModCode.Cards.Mod.Curse;
using CuteSakikoMod.CuteSakikoModCode.Monsters.Boss;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class AiHeartPower : CuteSakikoModPower
{
    private const string LiveScenePath = "res://CuteSakikoMod/scenes/monster/grey_anon_boss_live.tscn";
    private const string MusicFileName = "ai_heart.mp3";

    private NCreatureVisuals? _liveVisual;
    private NCreatureVisuals? _originalVisual;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromCard<Regreted>();
        }
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await ReplaceVisual();
        PlayMusic();

        if (Owner.Monster is GreyAnon greyAnon)
        {
            greyAnon.StartGreyText();
        }

        // 将拥有吉他的玩家的一个随机已记忆和弦替换为灰爱音和弦
        var combat = Owner.CombatState;
        if (combat != null)
        {
            const string chordId = "GreyAnonChord";
            foreach (var player in combat.Players)
            {
                var guitar = player.Relics.OfType<AnonGuitar>().FirstOrDefault();
                if (guitar != null)
                {
                    guitar.ReplaceRandomEquippedChord(chordId);
                }
            }
        }
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        RestoreVisual();
        // 不停止音乐，也不做任何标记
    }

    // 玩家造成伤害时，给攻击者抽牌堆添加 Regreted
    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || result.UnblockedDamage == 0) return;
        if (dealer?.Player == null) return;

        var combatState = Owner.CombatState;
        if (combatState == null) return;

        var regreted = combatState.CreateCard<Regreted>(dealer.Player);
        var addResult = await CardPileCmd.AddGeneratedCardToCombat(regreted, PileType.Draw, dealer.Player, CardPilePosition.Random);
        CardCmd.PreviewCardPileAdd(addResult);
    }

    // ===== 新功能：每回合结束时给持有吉他的玩家储存3个灰爱音和弦 =====
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        // 仅在敌方回合结束（即玩家回合结束）时执行
        if (side != CombatSide.Enemy) return;

        var combat = Owner.CombatState;
        if (combat == null) return;

        const string chordId = "GreyAnonChord";

        foreach (var player in combat.Players)
        {
            var guitar = player.Relics.OfType<AnonGuitar>().FirstOrDefault();
            if (guitar == null) continue;

            // 为该玩家构造合法的上下文
            var ctx = new HookPlayerChoiceContext(player, player.NetId, GameActionType.Combat);
            var task = guitar.AddChordToStored(ctx, chordId, 3);
            await ctx.AssignTaskAndWaitForPauseOrCompletion(task);
        }
    }

    private async Task ReplaceVisual()
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Owner);
        if (creatureNode == null) return;

        _originalVisual = creatureNode.GetChildren().OfType<NCreatureVisuals>().FirstOrDefault();
        if (_originalVisual != null)
        {
            _originalVisual.Visible = false;
            _originalVisual.Modulate = new Color(1, 1, 1, 0);
        }

        if (_liveVisual != null && GodotObject.IsInstanceValid(_liveVisual))
        {
            creatureNode.RemoveChild(_liveVisual);
            _liveVisual.QueueFree();
            _liveVisual = null;
        }

        var scene = GD.Load<PackedScene>(LiveScenePath);
        _liveVisual = scene.Instantiate<NCreatureVisuals>();
        _liveVisual.Name = "LiveVisual";

        var newBounds = _liveVisual.GetNodeOrNull<Control>("Bounds");
        if (newBounds != null)
            newBounds.MouseFilter = Control.MouseFilterEnum.Ignore;

        creatureNode.AddChild(_liveVisual);
        await Task.CompletedTask;
    }

    private void RestoreVisual()
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Owner);
        if (creatureNode == null) return;

        if (_liveVisual != null && GodotObject.IsInstanceValid(_liveVisual))
        {
            creatureNode.RemoveChild(_liveVisual);
            _liveVisual.QueueFree();
            _liveVisual = null;
        }

        if (_originalVisual != null && GodotObject.IsInstanceValid(_originalVisual))
        {
            _originalVisual.Visible = true;
            _originalVisual.Modulate = new Color(1, 1, 1, 1);
        }
        _originalVisual = null;
    }

    private void PlayMusic()
    {
        var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var fullPath = Path.Combine(dir, "audio", MusicFileName);
        AudioManager.PlayMusic(fullPath);
    }
}