using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace CuteSakikoMod.CuteSakikoModCode.NetMessage;

public sealed class RelicSwapChoiceMessage : INetMessage
{
    public ulong SourceNetId { get; set; }
    public List<string> RelicIds { get; set; } = new();

    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.Info;
    public bool ShouldBuffer => false;
    public bool ShouldBroadcast => true;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteULong(SourceNetId);
        writer.WriteString(string.Join("|", RelicIds ?? new List<string>()));
    }

    public void Deserialize(PacketReader reader)
    {
        SourceNetId = reader.ReadULong();
        var raw = reader.ReadString();
        RelicIds = string.IsNullOrEmpty(raw)
            ? new List<string>()
            : raw.Split('|').ToList();
    }

    // 注意：游戏的 INetMessage 接口没有 ToId，路由由 NetTypeCache 按类型分配；
    // 这里保留手写 ID 只为自描述与排查。改成 205 是为了不和 SkinSyncMessage 的 204 撞号。
    public int ToId()
    {
        return 205;
    }
}