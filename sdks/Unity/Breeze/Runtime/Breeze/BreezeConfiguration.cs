
public sealed class BreezeConfiguration
{
    public string AppScheme { get; set; }

    public BreezeEnvironment Environment { get; set; } = BreezeEnvironment.Production;
}

public enum BreezeEnvironment
{
    Production = 0,
    Development = 1,
}
