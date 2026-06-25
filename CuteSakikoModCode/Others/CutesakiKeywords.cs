using CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Common;
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
[RegisterOwnedCardKeyword(nameof(Sakiforget))]
[RegisterOwnedCardKeyword(nameof(Parfait))]
[RegisterOwnedCardKeyword(nameof(Neko),
    CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
[RegisterOwnedCardKeyword(nameof(RanaLive),
    CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
[RegisterOwnedCardKeyword(nameof(Pancake))]
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

    public static readonly string
        Sakiforget = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Sakiforget));
    public static readonly string
        Parfait = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Parfait));
    public static readonly string
        Neko = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Neko));
    public static readonly string
        RanaLive = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(RanaLive));
    public static readonly string
        Pancake = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Pancake));
}