using CuteSakikoMod.CuteSakikoModCode.Monsters.Weak;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;

namespace CuteSakikoMod.CuteSakikoModCode.Encounters.Event;

[RegisterGlobalEncounter]
public class LuoEncounter : ModEncounterTemplate
{
    public override bool IsValidForAct(ActModel act) => false;
    
    public override RoomType RoomType => RoomType.Monster;
    public override bool IsWeak => false;

    public override IReadOnlyList<string> Slots => new[]
    {
        "snail1", "snail2", "snail3",
        "snail4", "snail5", "snail6"
    };

    public override EncounterAssetProfile AssetProfile => new(
        EncounterScenePath: "res://CuteSakikoMod/scenes/encounter/luo_encounter.tscn"
    );

    public override float GetCameraScaling() => 0.9f;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<TianSuLuo>(),
        ModelDb.Monster<TianXiangLuo>(),
        ModelDb.Monster<Araluo>()
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        var pool = new List<MonsterModel>
        {
            ModelDb.Monster<TianSuLuo>(),
            ModelDb.Monster<TianXiangLuo>(),
            ModelDb.Monster<Araluo>()
        };

        var result = new List<(MonsterModel, string?)>();
        foreach (var slot in Slots)
        {
            var pick = pool[Rng.NextInt(pool.Count)];
            var mutable = pick.ToMutable();

            int initialIdx = Rng.NextInt(3);
            switch (mutable)
            {
                case TianSuLuo ts: ts.InitialIntentIndex = initialIdx; break;
                case TianXiangLuo tx: tx.InitialIntentIndex = initialIdx; break;
                case Araluo ar: ar.InitialIntentIndex = initialIdx; break;
            }

            result.Add((mutable, slot));
        }

        return result;
    }
}