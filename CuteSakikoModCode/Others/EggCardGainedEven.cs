// EggCardGainedEvent.cs

using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Others;

public static class EggCardGainedEvent
{
    public static event Action<CardModel> OnEggCardGained;

    public static void Trigger(CardModel card)
    {
        OnEggCardGained?.Invoke(card);
    }
}