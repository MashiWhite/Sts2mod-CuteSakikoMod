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
    
    // 大三和弦
[CustomEnum("Cchord")] [KeywordProperties(AutoKeywordPosition.None)]
public static CardKeyword Cchord;

[CustomEnum("Gchord")] [KeywordProperties(AutoKeywordPosition.None)]
public static CardKeyword Gchord;

[CustomEnum("Dchord")] [KeywordProperties(AutoKeywordPosition.None)]
public static CardKeyword Dchord;

[CustomEnum("Achord")] [KeywordProperties(AutoKeywordPosition.None)]
public static CardKeyword Achord;

[CustomEnum("Echord")] [KeywordProperties(AutoKeywordPosition.None)]
public static CardKeyword Echord;

[CustomEnum("C#chord")] [KeywordProperties(AutoKeywordPosition.None)]
public static CardKeyword CSharpChord;

[CustomEnum("D#chord")] [KeywordProperties(AutoKeywordPosition.None)]
public static CardKeyword DSharpChord;

// 小三和弦
[CustomEnum("Amchord")] [KeywordProperties(AutoKeywordPosition.None)]
public static CardKeyword Amchord;

[CustomEnum("Gmchord")] [KeywordProperties(AutoKeywordPosition.None)]
public static CardKeyword Gmchord;

[CustomEnum("Emchord")] [KeywordProperties(AutoKeywordPosition.None)]
public static CardKeyword Emchord;

[CustomEnum("Dmchord")] [KeywordProperties(AutoKeywordPosition.None)]
public static CardKeyword Dmchord;

[CustomEnum("Bmchord")] [KeywordProperties(AutoKeywordPosition.None)]
public static CardKeyword Bmchord;

[CustomEnum("C#mchord")] [KeywordProperties(AutoKeywordPosition.None)]
public static CardKeyword CSharpMChord;

[CustomEnum("D#mchord")] [KeywordProperties(AutoKeywordPosition.None)]
public static CardKeyword DSharpMChord;

// 属七和弦
[CustomEnum("G7chord")] [KeywordProperties(AutoKeywordPosition.None)]
public static CardKeyword G7Chord;

[CustomEnum("D7chord")] [KeywordProperties(AutoKeywordPosition.None)]
public static CardKeyword D7Chord;

[CustomEnum("A7chord")] [KeywordProperties(AutoKeywordPosition.None)]
public static CardKeyword A7Chord;

[CustomEnum("E7chord")] [KeywordProperties(AutoKeywordPosition.None)]
public static CardKeyword E7Chord;

[CustomEnum("C#7chord")] [KeywordProperties(AutoKeywordPosition.None)]
public static CardKeyword CSharp7Chord;

[CustomEnum("D#7chord")] [KeywordProperties(AutoKeywordPosition.None)]
public static CardKeyword DSharp7Chord;
}