using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace CuteSakikoMod.CuteSakikoModCode.Others;




public class CutesakiKeywords
{
    // 自定义枚举的名字。最终会变成{前缀}-{枚举值大写}的形式，例如TEST-UNIQUE
    [CustomEnum("Pressure")] [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword Pressure;

    // 自定义枚举的名字。最终会变成{前缀}-{枚举值大写}的形式，例如TEST-UNIQUE
    [CustomEnum("Memory")] [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword Memory;

    // 自定义枚举的名字。最终会变成{前缀}-{枚举值大写}的形式，例如TEST-UNIQUE
    [CustomEnum("Memorysaki")] [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword Memorysaki;

    // 自定义枚举的名字。最终会变成{前缀}-{枚举值大写}的形式，例如TEST-UNIQUE
    [CustomEnum("Sword")] [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword Sword;

    [CustomEnum("Eggs")] [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword Eggs;

    [CustomEnum("Nochest")] [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword Nochest;

    [CustomEnum("Playpiano")] [KeywordProperties(AutoKeywordPosition.None)]
    public static CardKeyword Playpiano;
    
    [CustomEnum("Playguitar")] [KeywordProperties(AutoKeywordPosition.None)]
    public static CardKeyword Playguitar;

    [CustomEnum("NoNote")] [KeywordProperties(AutoKeywordPosition.None)]
    public static CardKeyword NoNote;
    
    [CustomEnum("OtherAnon")] [KeywordProperties(AutoKeywordPosition.None)]
    public static CardKeyword OtherAnon;
    
    [CustomEnum("Chord")] [KeywordProperties(AutoKeywordPosition.None)]
    public static CardKeyword Chord;
    
    [CustomEnum("RememberChord")] [KeywordProperties(AutoKeywordPosition.None)]
    public static CardKeyword RememberChord;
}