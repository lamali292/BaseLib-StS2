using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace BaseLib.Cards.Variables;

/// <summary>
/// A calculated var that allows multiple on a single model and works on relics and powers.
/// </summary>
public class CustomCalculatedVar : CalculatedVar
{
    public CustomCalculatedVar(string name) : base(name)
    {
        BaseLibMain.Logger.Info($"CustomCalculatedVar: {Name}");
    }

    protected override DynamicVar GetBaseVar()
    {
        return _owner!.GetDynamicVar($"{Name}Base");
    }

    protected override DynamicVar GetExtraVar()
    {
        return _owner!.GetDynamicVar($"{Name}Extra");
    }
}