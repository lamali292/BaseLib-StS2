namespace BaseLib.Config;

[AttributeUsage(AttributeTargets.Property)]
public class ConfigSectionAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}