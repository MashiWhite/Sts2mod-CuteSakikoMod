using System.Threading;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

/// <summary>
///     言刃/破防的固定真实伤害作用域。
///     只在本模组主动结算这段伤害期间为 true；
///     补丁据此放行 HardenedShell，不依赖 ValueProp 的自定义位，避免和其他 mod 撞位。
/// </summary>
public static class WordBladeDamageScope
{
    private static readonly AsyncLocal<int> Depth = new();

    public static bool IsActive => Depth.Value > 0;

    public static IDisposable Enter()
    {
        Depth.Value++;
        return new Scope();
    }

    private sealed class Scope : IDisposable
    {
        public void Dispose()
        {
            if (Depth.Value > 0) Depth.Value--;
        }
    }
}
