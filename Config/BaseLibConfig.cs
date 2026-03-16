using MegaCrit.Sts2.Core.Entities.Cards;

namespace BaseLib.Config;

internal class BaseLibConfig : SimpleModConfig
{
    [ConfigSection("ExampleSectionOne")]
    public static CardKeyword Keyword { get; set; } = CardKeyword.None;
    public static bool Test { get; set; } = true;
    public static bool AnotherWithMultipleWords { get; set; } = false;
    public static bool More { get; set; } = false;

    [ConfigSection("Example Section Two")]
    public static bool One { get; set; } = true;
    public static bool Two { get; set; } = false;
    public static bool Three { get; set; } = true;
    /*
    public static bool Testing { get; set; } = true;
    public static bool A { get; set; } = true;
    public static bool B { get; set; } = true;
    public static bool C { get; set; } = true;
    public static bool D { get; set; } = true;
    public static bool E { get; set; } = true;
    public static bool F { get; set; } = true;
    public static bool G { get; set; } = true;
    public static bool H { get; set; } = true;
    public static bool I { get; set; } = true;
    public static bool J { get; set; } = true;*/
}