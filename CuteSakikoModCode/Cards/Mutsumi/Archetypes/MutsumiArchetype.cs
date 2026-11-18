namespace CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Archetypes;

/// <summary>睦的流派（卡池倾向）。与初始遗物/牌组无关，玩家可自由组合。</summary>
public enum MutsumiArchetype
{
    /// <summary>黄瓜睦：沉默、种植、技能/防御倾向。</summary>
    Cucumber = 0,

    /// <summary>Mortis：多言、社交、攻击牌倾向。</summary>
    Mortis = 1
}

/// <summary>给卡牌打流派标签；不实现该接口 = 通用卡，两个流派都能拿到。</summary>
public interface IMutsumiArchetypeCard
{
    MutsumiArchetype Archetype { get; }
}
