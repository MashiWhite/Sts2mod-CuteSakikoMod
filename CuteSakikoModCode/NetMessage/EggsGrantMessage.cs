using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace CuteSakikoMod.CuteSakikoModCode.NetMessage;

/// <summary>
/// 房主广播：给指定玩家（TargetPlayerNetId）发放 Eggs 遗物。
/// 客户端收到后本地执行 RelicCmd.Obtain，保证两端状态一致。
/// </summary>
public sealed class EggsGrantMessage : INetMessage
{
    /// <summary>要发放 Eggs 的玩家 netId。</summary>
    public ulong TargetPlayerNetId { get; set; }

    public bool ShouldBroadcast => true;

    public NetTransferMode Mode => NetTransferMode.Reliable;

    public LogLevel LogLevel => LogLevel.VeryDebug;

    public bool ShouldBuffer => true;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteULong(TargetPlayerNetId);
    }

    public void Deserialize(PacketReader reader)
    {
        TargetPlayerNetId = reader.ReadULong();
    }
}