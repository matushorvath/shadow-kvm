namespace ShadowKVM.Tests;

// TODO remove this class and this App instance
public class AppFixture
{
    public App App = new App();
}

[CollectionDefinition("AppFixture")]
public class AppCollection : ICollectionFixture<AppFixture>
{
}
