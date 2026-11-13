using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace CuteSakikoMod.CuteSakikoModCode.Nodes.Combat;

/// <summary>
///     带自定义图标 + 居中数字的意图基类（继承 StatusIntent 以复用数字显示）。
///     图标由 <see cref="NIntentCustomCountIntentPatch" /> 直接写入 NIntent 的 Sprite2D。
/// </summary>
public abstract class CustomCountIntent : StatusIntent
{
    private readonly int _amount;

    protected CustomCountIntent(int amount) : base(amount)
    {
        _amount = amount;
    }

    protected abstract string CountPrefix { get; }

    protected abstract string IconPath { get; }

    public int Count => _amount;
    public virtual bool ShowCount => true;

    public Texture2D? IconTexture => GD.Load<Texture2D>(IconPath);

    // 不要返回绝对路径：AbstractIntent.AssetPaths 会再拼一次 res://images/，导致加载失败\n    protected override string? SpritePath => null;

    protected override string IntentPrefix => CountPrefix;

    // 原版回合动画表里没有我们的键，必须避开 IntentAnimData 的查找
    public override string GetAnimation(IEnumerable<Creature> targets, Creature owner) => "status";

    public override Texture2D? GetTexture(IEnumerable<Creature> targets, Creature owner) => IconTexture;

    public override LocString GetIntentLabel(IEnumerable<Creature> targets, Creature owner)
    {
        var loc = new LocString("intents", "FORMAT_COUNT");
        loc.Add("Count", Count);
        return loc;
    }

    protected override LocString GetIntentDescription(IEnumerable<Creature> targets, Creature owner)
    {
        var loc = new LocString("intents", CountPrefix + ".description");
        loc.Add("Count", Count);
        return loc;
    }
}

/// <summary>飞返计数意图：显示这个敌人下回合会让你的飞返计数增加多少。</summary>
public sealed class FlybackCountIntent(int amount) : CustomCountIntent(amount)
{
    protected override string CountPrefix => "FLYBACK_COUNT";
    protected override string IconPath => "res://CuteSakikoMod/images/powers/RetrogradePower.png";
}

/// <summary>Reload 计数意图：显示这个敌人下回合会让你的 Reload 计数增加多少。</summary>
public sealed class ReloadCountIntent(int amount) : CustomCountIntent(amount)
{
    protected override string CountPrefix => "RELOAD_COUNT";
    protected override string IconPath => "res://CuteSakikoMod/images/powers/TimeWatchPower.png";
}
/// <summary>飞返打出次数翻倍意图：图标-only，不显示数字。</summary>
public sealed class FlybackDoubleIntent() : CustomCountIntent(0)
{
    protected override string CountPrefix => "FLYBACK_DOUBLE";
    protected override string IconPath => "res://CuteSakikoMod/images/powers/LastRetrogradePower.png";
    public override bool ShowCount => false;

    // 连兜底也不显示数字：即使补丁没接管，默认 StatusIntent 分支也拿不到角标文本
    public override LocString GetIntentLabel(IEnumerable<Creature> targets, Creature owner) =>
        new LocString("intents", "FORMAT_EMPTY");
}
