using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace CuteSakikoMod.CuteSakikoModCode.NetMessage
{
    public sealed class NameChangeMessage : INetMessage
    {
        public ulong TargetNetId { get; set; }
        public string NewName { get; set; }

        public NetTransferMode Mode => NetTransferMode.Reliable;
        public LogLevel LogLevel => LogLevel.Info;
        public bool ShouldBuffer => false;
        public bool ShouldBroadcast => true;

        public void Serialize(PacketWriter writer)
        {
            writer.WriteULong(TargetNetId);
            writer.WriteString(NewName ?? string.Empty);
        }

        public void Deserialize(PacketReader reader)
        {
            TargetNetId = reader.ReadULong();
            NewName = reader.ReadString();
        }

        public int ToId() => 203;
    }
}