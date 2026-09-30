namespace LightSpace.Core;

public static class VirtualCopyNames
{
    public const int MaximumLength = 100;
    public static string Validate(string name)
    {
        ArgumentNullException.ThrowIfNull(name); name = name.Trim();
        if (name.Length is < 1 or > MaximumLength || name.Any(char.IsControl))
            throw new ArgumentException("A copy name must contain 1–100 visible characters.", nameof(name));
        return name;
    }
}
