using STS2RitsuLib.Scaffolding.Characters;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;

namespace CuteSakikoMod.CuteSakikoModCode.Character;

// 泛型参数与 ModCharacterTemplate 一致
public abstract class CuteSakikoCharacter<TCardPool, TRelicPool, TPotionPool>
    : ModCharacterTemplate<TCardPool, TRelicPool, TPotionPool>
    where TCardPool : CardPoolModel
    where TRelicPool : RelicPoolModel
    where TPotionPool : PotionPoolModel
{
    // 这里可以放置你的角色共有的自定义逻辑
}