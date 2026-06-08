using CuteSakikoMod.CuteSakikoModCode.Cards.Mod.Curse;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using System.Reflection;
using CuteSakikoMod.CuteSakikoModCode.Monsters.Boss;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using System.Linq;
using MegaCrit.Sts2.Core.Nodes.Rooms; // 确保引入

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class AiHeartPower : CuteSakikoModPower
{
    private const string LiveScenePath = "res://CuteSakikoMod/scenes/monster/grey_anon_boss_live.tscn";
    private const string MusicFileName = "ai_heart.mp3";

    private NCreatureVisuals? _liveVisual;       // 新添加的 Live 视觉
    private NCreatureVisuals? _originalVisual;   // 隐藏的原始视觉

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
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        RestoreVisual();
        AudioManager.StopMusic();
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
        var addResult = await CardPileCmd.AddGeneratedCardToCombat(regreted, PileType.Draw, dealer.Player);
        CardCmd.PreviewCardPileAdd(addResult);
    }

    private async Task ReplaceVisual()
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Owner);
        if (creatureNode == null) return;

        // 缓存旧视觉（不删除！）
        _originalVisual = creatureNode.GetChildren().OfType<NCreatureVisuals>().FirstOrDefault();
        if (_originalVisual != null)
        {
            _originalVisual.Visible = false;
            _originalVisual.Modulate = new Color(1, 1, 1, 0);
        }

        // 移除旧的 Live 视觉（如果存在）
        if (_liveVisual != null && GodotObject.IsInstanceValid(_liveVisual))
        {
            creatureNode.RemoveChild(_liveVisual);
            _liveVisual.QueueFree();
            _liveVisual = null;
        }

        // 加载新视觉
        var scene = GD.Load<PackedScene>(LiveScenePath);
        _liveVisual = scene.Instantiate<NCreatureVisuals>();
        _liveVisual.Name = "LiveVisual";

        // ★ 禁用新视觉的 Bounds 节点，避免阻挡鼠标事件
        var newBounds = _liveVisual.GetNodeOrNull<Control>("Bounds"); // 假设唯一名称是 Bounds
        if (newBounds != null)
            newBounds.MouseFilter = Control.MouseFilterEnum.Ignore;

        // 同样禁用其他可能接收鼠标的节点
        // IntentPos、TalkPos 等只是 Marker2D，不影响，但如果你有额外的 Control，也一并处理

        creatureNode.AddChild(_liveVisual);

        await Task.CompletedTask;
    }

    private void RestoreVisual()
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Owner);
        if (creatureNode == null) return;

        // 移除 Live 视觉
        if (_liveVisual != null && GodotObject.IsInstanceValid(_liveVisual))
        {
            creatureNode.RemoveChild(_liveVisual);
            _liveVisual.QueueFree();
            _liveVisual = null;
        }

        // 恢复原始视觉
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