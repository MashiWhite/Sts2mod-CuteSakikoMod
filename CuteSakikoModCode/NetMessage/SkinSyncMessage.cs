using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace CuteSakikoMod.CuteSakikoModCode.NetMessage;

public sealed class SkinSyncMessage : INetMessage
{
    public ulong SourceNetId { get; set; }
    public string CharacterTypeName { get; set; } = "";
    public int ArtSkinIndex { get; set; }
    public int DeckPresetIndex { get; set; }
    public int RelicPresetIndex { get; set; }
    public int MutsumiArchetypeIndex { get; set; }

    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.Info;
    public bool ShouldBuffer => false;
    public bool ShouldBroadcast => true;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteULong(SourceNetId);
        writer.WriteString(CharacterTypeName ?? string.Empty);
        writer.WriteInt(ArtSkinIndex);
        writer.WriteInt(DeckPresetIndex);
        writer.WriteInt(RelicPresetIndex);
        writer.WriteInt(MutsumiArchetypeIndex);
    }

    public void Deserialize(PacketReader reader)
    {
        SourceNetId = reader.ReadULong();
        CharacterTypeName = reader.ReadString();
        ArtSkinIndex = reader.ReadInt();
        DeckPresetIndex = reader.ReadInt();
        RelicPresetIndex = reader.ReadInt();
        MutsumiArchetypeIndex = reader.ReadInt();
    }

    public int ToId()
    {
        return 204;
    }
}