namespace CuteSakikoMod.CuteSakikoModCode.Singletons
{
    /// <summary>跑局级共享数据（读档次数）</summary>
    public class RunFlybackData
    {
        public int ReloadCount { get; set; }
    }

    /// <summary>玩家级独立数据（飞返打出次数）</summary>
    public class PlayerFlybackData
    {
        public int PlayCount { get; set; }
    }
}