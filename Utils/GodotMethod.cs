using Godot;

namespace BaseLib.Utils;

/// <summary>
/// Simple way of declaring a Godot method that can be called through GodotObject.Call.
/// Recommended to store in variable of type GodotMethodDelegate for easy invocation.
/// </summary>
public class GodotMethod(StringName name)
{
    public Variant Invoke(GodotObject obj, params Variant[] args)
    {
        return obj.Call(name, args);
    }

    public static implicit operator GodotMethodDelegate(GodotMethod godotMethod) => godotMethod.AsDelegate();

    public GodotMethodDelegate AsDelegate() => Invoke;
}

public delegate Variant GodotMethodDelegate(GodotObject obj, params Variant[] args);