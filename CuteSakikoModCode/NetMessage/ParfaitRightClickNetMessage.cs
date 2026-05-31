using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace CuteSakikoMod.CuteSakikoModCode.NetMessage;

public class ParfaitRightClickNetMessage : INetMessage
{
    public ulong PlayerNetId { get; set; }

    public bool ShouldBroadcast => false;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.Info;
    public bool ShouldBuffer => false;

    public void Serialize(PacketWriter writer) => writer.WriteULong(PlayerNetId);
    public void Deserialize(PacketReader reader) => PlayerNetId = reader.ReadULong();
}