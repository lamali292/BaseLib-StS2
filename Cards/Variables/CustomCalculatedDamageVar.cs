using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace BaseLib.Cards.Variables;

/// <summary>
/// A CalculatedDamageVar that allows a custom name and can have multiple on one model.
/// Also works on relics and powers.
/// </summary>
public class CustomCalculatedDamageVar : CalculatedDamageVar
{
    private static Action<DynamicVar, string>? _nameSetter = ReflectionUtils.GetSetterForProperty<DynamicVar, string>("Name");
    public CustomCalculatedDamageVar(string name, ValueProp props) : base(props)
    {
        _nameSetter?.Invoke(this, name);
        BaseLibMain.Logger.Info($"CustomCalculatedDamageVar: {Name}");
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