using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace CuteSakikoMod.CuteSakikoModCode.NetMessage;

/// <summary>主机广播当前总飞返次数给所有客户端</summary>
public sealed class PlayCountSyncMessage : INetMessage
{
    public int TotalPlayCount { get; set; }

    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.Info;
    public bool ShouldBuffer => false;
    public bool ShouldBroadcast => true;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt(TotalPlayCount);
    }

    public void Deserialize(PacketReader reader)
    {
        TotalPlayCount = reader.ReadInt();
    }

    public int ToId()
    {
        return 201; // 避开 200 的 ReloadCountSyncMessage
    }
}