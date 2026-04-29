using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Others;

[RegisterOwnedCardKeyword(nameof(Pressure))]
[RegisterOwnedCardKeyword(nameof(Memory),
    CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.AfterCardDescription)]
[RegisterOwnedCardKeyword(nameof(Sword))]
[RegisterOwnedCardKeyword(nameof(Eggs))]
[RegisterOwnedCardKeyword(nameof(Nochest))]
[RegisterOwnedCardKeyword(nameof(Playpiano))]
[RegisterOwnedCardKeyword(nameof(Playguitar))]
[RegisterOwnedCardKeyword(nameof(NoNote),
    CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
[RegisterOwnedCardKeyword(nameof(OtherAnon))]
[RegisterOwnedCardKeyword(nameof(Chord))]
[RegisterOwnedCardKeyword(nameof(RememberChord))]
[RegisterOwnedCardKeyword(nameof(Memorysaki))]
public class CutesakiKeywords
{
    public static readonly string Pressure = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Pressure));
    public static readonly string Memory = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Memory));
    public static readonly string Sword = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Sword));
    public static readonly string Eggs = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Eggs));
    public static readonly string Nochest = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Nochest));
    public static readonly string Playpiano = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Playpiano));

    public static readonly string
        Playguitar = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Playguitar));

    public static readonly string NoNote = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(NoNote));
    public static readonly string OtherAnon = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(OtherAnon));
    public static readonly string Chord = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Chord));

    public static readonly string RememberChord =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(RememberChord));

    public static readonly string
        Memorysaki = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Memorysaki));
}