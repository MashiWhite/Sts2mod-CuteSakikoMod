using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace CuteSakikoMod.CuteSakikoModCode.NetMessage;

/// <summary>主机广播当前读档次数给所有客户端</summary>
public sealed class ReloadCountSyncMessage : INetMessage
{
    public int ReloadCount { get; set; }

    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.Info; // 注意类型是 LogLevel 枚举
    public bool ShouldBuffer => false;
    public bool ShouldBroadcast => true;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt(ReloadCount);
    }

    public void Deserialize(PacketReader reader)
    {
        ReloadCount = reader.ReadInt();
    }

    // 自定义消息 ID，避开官方占用的 ID（官方 0-99 保留，我们从 200 开始）
    public int ToId()
    {
        return 200;
    }
}