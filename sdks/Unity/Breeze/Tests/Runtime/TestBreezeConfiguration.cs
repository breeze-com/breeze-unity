using NUnit.Framework;

/// <summary>
/// Tests for BreezeConfiguration and BreezeEnvironment.
/// </summary>
public class TestBreezeConfiguration
{
    [Test]
    public void DefaultEnvironment_IsProduction()
    {
        var config = new BreezeConfiguration();
        Assert.AreEqual(BreezeEnvironment.Production, config.Environment);
    }

    [Test]
    public void DefaultAppScheme_IsNull()
    {
        var config = new BreezeConfiguration();
        Assert.IsNull(config.AppScheme);
    }

    [Test]
    public void AppScheme_SetGet()
    {
        var config = new BreezeConfiguration { AppScheme = "mygame" };
        Assert.AreEqual("mygame", config.AppScheme);
    }

    [Test]
    public void Environment_SetDevelopment()
    {
        var config = new BreezeConfiguration { Environment = BreezeEnvironment.Development };
        Assert.AreEqual(BreezeEnvironment.Development, config.Environment);
    }

    [Test]
    public void Environment_SetProduction()
    {
        var config = new BreezeConfiguration { Environment = BreezeEnvironment.Production };
        Assert.AreEqual(BreezeEnvironment.Production, config.Environment);
    }

    [Test]
    public void AppScheme_CanBeOverwritten()
    {
        var config = new BreezeConfiguration { AppScheme = "first" };
        config.AppScheme = "second";
        Assert.AreEqual("second", config.AppScheme);
    }

    [Test]
    public void Environment_CanBeOverwritten()
    {
        var config = new BreezeConfiguration { Environment = BreezeEnvironment.Development };
        config.Environment = BreezeEnvironment.Production;
        Assert.AreEqual(BreezeEnvironment.Production, config.Environment);
    }

    [Test]
    public void AppScheme_EmptyString_Allowed()
    {
        var config = new BreezeConfiguration { AppScheme = "" };
        Assert.AreEqual("", config.AppScheme);
    }

    [Test]
    public void AppScheme_WithSchemeChars()
    {
        var config = new BreezeConfiguration { AppScheme = "com.breeze.game" };
        Assert.AreEqual("com.breeze.game", config.AppScheme);
    }

    [Test]
    public void MultipleInstances_Independent()
    {
        var a = new BreezeConfiguration { AppScheme = "a", Environment = BreezeEnvironment.Production };
        var b = new BreezeConfiguration { AppScheme = "b", Environment = BreezeEnvironment.Development };
        Assert.AreEqual("a", a.AppScheme);
        Assert.AreEqual("b", b.AppScheme);
        Assert.AreEqual(BreezeEnvironment.Production, a.Environment);
        Assert.AreEqual(BreezeEnvironment.Development, b.Environment);
    }
}
