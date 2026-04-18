using BaseLib.Abstracts;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;

namespace CuteSakikoMod.CuteSakikoModCode.Potions;

[Pool(typeof(CuteSakiPotionPool))]
public abstract class CuteSakikoModPotion : CustomPotionModel;