namespace CuteSakikoMod.CuteSakikoModCode.Systems.Skin.Core;

public sealed class SkinSelectionState
{
    // key = Type.FullName
    public Dictionary<string, CharacterSkinChoice> Choices { get; set; } = [];
}

public sealed class CharacterSkinChoice
{
    public int ArtSkinIndex { get; set; }
    public int DeckPresetIndex { get; set; }
    public int RelicPresetIndex { get; set; }

    /// <summary>睦的卡池流派：0=Cucumber/小睦，1=Mortis/莫提斯。其他角色忽略。</summary>
    public int MutsumiArchetypeIndex { get; set; }

    public CharacterSkinChoice Clone()
    {
        return new CharacterSkinChoice
        {
            ArtSkinIndex = ArtSkinIndex,
            DeckPresetIndex = DeckPresetIndex,
            RelicPresetIndex = RelicPresetIndex,
            MutsumiArchetypeIndex = MutsumiArchetypeIndex
        };
    }
}