using BaseLib.Abstracts;
using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Basic;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Pools.Anon;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Basic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
// 引入 Entry 命名空间

namespace CuteSakikoMod.CuteSakikoModCode.Character;





public class CuteAnon : PlaceholderCharacterModel
{
    public const string CharacterId = "千早爱音";
    public static readonly Color Color = new("#ff8899");
    public override Color EnergyLabelOutlineColor => new(0f, 0.2f, 0.4f);

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Feminine;
    public override int StartingHp => 70;

    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<AnonStrike>(),
        ModelDb.Card<AnonStrike>(),
        ModelDb.Card<AnonStrike>(),
        ModelDb.Card<AnonStrike>(),
        ModelDb.Card<AnonDefend>(),
        ModelDb.Card<AnonDefend>(),
        ModelDb.Card<AnonDefend>(),
        ModelDb.Card<AnonDefend>(),
        ModelDb.Card<PlayChord>(),
    ];

    public override IReadOnlyList<RelicModel> StartingRelics
    {
        get
        {
            var relics = new List<RelicModel>
            {
                ModelDb.Relic<AnonGuitar>()
            };
            return relics;
        }
    }

    public override CardPoolModel CardPool => ModelDb.CardPool<CuteAnonCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<CuteAnonRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<CuteAnonPotionPool>();

    public override string CustomIconPath => "res://CuteSakikoMod/scenes/char/anon/anon_icon.tscn";
    public override string CustomCharacterSelectBg => "res://CuteSakikoMod/scenes/char/anon/anon_bg.tscn";
    public override string CustomVisualPath => "res://CuteSakikoMod/scenes/char/anon/anon.tscn";
    public override string CustomMerchantAnimPath => "res://CuteSakikoMod/scenes/char/anon/anon_merchant.tscn";
    public override string CustomRestSiteAnimPath => "res://CuteSakikoMod/scenes/char/anon/anon_rest_site.tscn";
    public override string CustomArmPointingTexturePath => "res://CuteSakikoMod/images/others/anon/multiplayer_hand_point.png";
    public override string CustomArmRockTexturePath => "res://CuteSakikoMod/images/others/anon/multiplayer_hand_rock.png";
    public override string CustomArmPaperTexturePath => "res://CuteSakikoMod/images/others/anon/multiplayer_hand_paper.png";
    public override string CustomArmScissorsTexturePath => "res://CuteSakikoMod/images/others/anon/multiplayer_hand_scissors.png";
    public override string CustomEnergyCounterPath => "res://CuteSakikoMod/scenes/char/anon/anon_energy_counter.tscn";

    public override Color DialogueColor => new("#ff8899");
    public override Color MapDrawingColor => new("#ff8899");
    public override Color RemoteTargetingLineColor => new("#ff8899");
    public override Color RemoteTargetingLineOutline => new("#ff8899");

    public override string CustomIconTexturePath => "anon/character_icon_anon.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "anon/char_select_anon.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "anon/char_select_anon_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "anon/map_marker_anon.png".CharacterUiPath();
}