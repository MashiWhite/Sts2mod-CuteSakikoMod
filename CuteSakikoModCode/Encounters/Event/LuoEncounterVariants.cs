using STS2RitsuLib.Interop.AutoRegistration;

namespace CuteSakikoMod.CuteSakikoModCode.Encounters.Event;

/// <summary>炸虾事件里"穿过灌木"分支使用的罗怪遭遇。</summary>
[RegisterGlobalEncounter]
public sealed class LuoEncounterCrossBush : LuoEncounter { }

/// <summary>炸虾事件里"在此休息"分支使用的罗怪遭遇。</summary>
[RegisterGlobalEncounter]
public sealed class LuoEncounterRestHere : LuoEncounter { }