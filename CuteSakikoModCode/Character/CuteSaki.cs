using BaseLib.Abstracts;
using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Basic;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
using CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Basic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
// 引入 Entry 命名空间

namespace CuteSakikoMod.CuteSakikoModCode.Character;



public class CuteSaki : PlaceholderCharacterModel
{
    public const string CharacterId = "丰川祥子";
    public const string CharacterEggId = "小祥彩蛋";
    public static readonly Color Color = new("#7799cc");
    public override Color EnergyLabelOutlineColor => new(1f, 0f, 0f);

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Feminine;
    public override int StartingHp => 65;

    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<StrikeFast>(),
        ModelDb.Card<StrikeFast>(),
        ModelDb.Card<StrikeSlow>(),
        ModelDb.Card<StrikeSlow>(),
        ModelDb.Card<DefendSaki>(),
        ModelDb.Card<DefendSaki>(),
        ModelDb.Card<DefendSaki>(),
        ModelDb.Card<DefendSaki>(),
        ModelDb.Card<GoWork>(),
        ModelDb.Card<Work>()
    ];

    public override IReadOnlyList<RelicModel> StartingRelics
    {
        get
        {
            var relics = new List<RelicModel>
            {
                ModelDb.Relic<KabutoNote>()
            };
            return relics;
        }
    }

    public override CardPoolModel CardPool => ModelDb.CardPool<CuteSakiCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<CuteSakiRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<CuteSakiPotionPool>();

    public override string CustomIconPath => "res://CuteSakikoMod/scenes/char/saki/saki_icon.tscn";
    public override string CustomCharacterSelectBg => "res://CuteSakikoMod/scenes/char/saki/saki_bg.tscn";
    public override string CustomVisualPath => "res://CuteSakikoMod/scenes/char/saki/sakiko.tscn";
    public override string CustomMerchantAnimPath => "res://CuteSakikoMod/scenes/char/saki/saki_merchant.tscn";
    public override string CustomRestSiteAnimPath => "res://CuteSakikoMod/scenes/char/saki/saki_rest_site.tscn";
    public override string CustomArmPointingTexturePath => "res://CuteSakikoMod/images/others/saki/multiplayer_hand_point.png";
    public override string CustomArmRockTexturePath => "res://CuteSakikoMod/images/others/saki/multiplayer_hand_rock.png";
    public override string CustomArmPaperTexturePath => "res://CuteSakikoMod/images/others/saki/multiplayer_hand_paper.png";
    public override string CustomArmScissorsTexturePath => "res://CuteSakikoMod/images/others/saki/multiplayer_hand_scissors.png";
    public override string CustomEnergyCounterPath => "res://CuteSakikoMod/scenes/char/saki/saki_energy_counter.tscn";

    public override Color DialogueColor => new("#7799cc");
    public override Color MapDrawingColor => new("#7799cc");
    public override Color RemoteTargetingLineColor => new("#7799cc");
    public override Color RemoteTargetingLineOutline => new("#7799cc");

    public override string CustomIconTexturePath => "saki/character_icon_saki.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "saki/char_select_saki.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "saki/char_select_saki_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "saki/map_marker_saki.png".CharacterUiPath();
}