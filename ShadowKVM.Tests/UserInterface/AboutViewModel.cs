using Moq;

namespace ShadowKVM.Tests;

public class AboutViewModelTests
{
    Mock<IUrlOpener> _urlOpenerMock = new();

    [Fact]
    public void Close_WithNoEventHandler()
    {
        var model = new AboutViewModel(_urlOpenerMock.Object);
        model.Close();
    }

    [Fact]
    public void Close_CallsEventHandler()
    {
        var model = new AboutViewModel(_urlOpenerMock.Object);

        var called = false;
        model.RequestClose += (sender, args) =>
        {
            Assert.Equal(model, sender);
            Assert.Equal(args, EventArgs.Empty);
            called = true;
        };

        model.Close();

        Assert.True(called);
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
            var model = new AboutViewModel(_urlOpenerMock.Object);
            version = model.Version;
        }

        Assert.Matches(@"^\d+\.\d+\.\d+(-\w+(\.\d+(\+\d+)?)?)?$", version);
    }

    [Fact]
    public void OpenLicense_CallsUrlOpener()
    {
        _urlOpenerMock
            .Setup(m => m.Open("https://opensource.org/license/mit"))
            .Verifiable();

        var model = new AboutViewModel(_urlOpenerMock.Object);
        model.OpenLicense();

        _urlOpenerMock.Verify();
    }

    [Fact]
    public void OpenManual_CallsUrlOpener()
    {
        _urlOpenerMock
            .Setup(m => m.Open("https://github.com/matushorvath/shadow-kvm#readme-ov-file"))
            .Verifiable();

        var model = new AboutViewModel(_urlOpenerMock.Object);
        model.OpenManual();

        _urlOpenerMock.Verify();
    }

    [Fact]
    public void OpenReleases_CallsUrlOpener()
    {
        _urlOpenerMock
            .Setup(m => m.Open("https://github.com/matushorvath/shadow-kvm/releases"))
            .Verifiable();

        var model = new AboutViewModel(_urlOpenerMock.Object);
        model.OpenReleases();

        _urlOpenerMock.Verify();
    }
}
