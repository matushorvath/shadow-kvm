using Moq;

namespace ShadowKVM.Tests;

public class AboutViewModelTests
{
    Mock<INativeUserInterface> _nativeUserInterfaceMock = new();

    [Fact]
    public void Construct_WithDefaultServices()
    {
        new AboutViewModel();
    }

    [Fact]
    public void Close_WithNoEventHandler()
    {
        var model = new AboutViewModel(_nativeUserInterfaceMock.Object);
        model.CloseCommand.Execute(null);
    }

    [Fact]
    public void Close_CallsEventHandler()
    {
        var model = new AboutViewModel(_nativeUserInterfaceMock.Object);

        var eventTriggered = false;
        model.RequestClose += sender => { eventTriggered = true; };

        model.CloseCommand.Execute(null);

        Assert.True(eventTriggered);
    }

    [Theory]
    [InlineData("1.2.3")]
    [InlineData("1.2.3-text")]
    [InlineData("1.2.3-text.4")]
    [InlineData("1.2.3-text.4+5")]
    [InlineData(default)]
    public void GitVersionInformation_LooksCorrect(string? version)
    {
        // We check the same regex with various valid version strings, to make sure
        // the test will not start failing in main where the version format is different
        // (basically we are testing this test, because it needs to work with various inputs)
        if (version == default)
        {
            var model = new AboutViewModel(_nativeUserInterfaceMock.Object);
            version = model.Version;
        }

        Assert.Matches(@"^\d+\.\d+\.\d+(-.+(\.\d+(\+\d+)?)?)?$", version);
    }

    [Fact]
    public void OpenLicense_CallsUrlOpener()
    {
        _nativeUserInterfaceMock
            .Setup(m => m.OpenUrl("https://opensource.org/license/mit"))
            .Verifiable();

        var model = new AboutViewModel(_nativeUserInterfaceMock.Object);
        model.OpenLicenseCommand.Execute(null);

        _nativeUserInterfaceMock.Verify();
    }

    [Fact]
    public void OpenManual_CallsUrlOpener()
    {
        _nativeUserInterfaceMock
            .Setup(m => m.OpenUrl("https://github.com/matushorvath/shadow-kvm#readme-ov-file"))
            .Verifiable();

        var model = new AboutViewModel(_nativeUserInterfaceMock.Object);
        model.OpenManualCommand.Execute(null);

        _nativeUserInterfaceMock.Verify();
    }

    [Fact]
    public void OpenReleases_CallsUrlOpener()
    {
        _nativeUserInterfaceMock
            .Setup(m => m.OpenUrl("https://github.com/matushorvath/shadow-kvm/releases"))
            .Verifiable();

        var model = new AboutViewModel(_nativeUserInterfaceMock.Object);
        model.OpenReleasesCommand.Execute(null);

        _nativeUserInterfaceMock.Verify();
    }
}
