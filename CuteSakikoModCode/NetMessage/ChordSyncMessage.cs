using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace CuteSakikoMod.CuteSakikoModCode.NetMessage;

public class ChordSyncMessage : INetMessage
{
    public ulong PlayerNetId;
    public string ChordsData;
    public string BonusChordsData;
    public string LearnedChordsData;

    public bool ShouldBroadcast => true; // 关键：允许广播
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.VeryDebug;
    public bool ShouldBuffer => true;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteULong(PlayerNetId);
        writer.WriteString(ChordsData ?? "");
        writer.WriteString(BonusChordsData ?? "");
        writer.WriteString(LearnedChordsData ?? "");
    }

    public void Deserialize(PacketReader reader)
    {
        PlayerNetId = reader.ReadULong();
        ChordsData = reader.ReadString();
        BonusChordsData = reader.ReadString();
        LearnedChordsData = reader.ReadString();
    }
}