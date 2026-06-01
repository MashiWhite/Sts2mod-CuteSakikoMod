namespace CuteSakikoMod.CuteSakikoModCode.Singletons;

/// <summary>跑局级共享数据（读档计数相关）</summary>
public class RunFlybackData
{
    public int ExtraReloadNum { get; set; }   // 本局额外增加的读档数（等同于原来的 _extraReloadNum）
    public int BaseReloadCount { get; set; }  // 本局的基准读档数（主机为 RunManager._numReloads，客户端为主机同步的值）
}

/// <summary>玩家级独立数据（飞返打出次数）</summary>
public class PlayerFlybackData
{
    public int PlayCount { get; set; }
}