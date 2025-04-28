using Moq;

namespace ShadowKVM.Tests;

public class NotifyIconViewModelTests
{
    Mock<IAppControl> _appControlMock = new();
    Mock<IAutostart> _autostartMock = new();
    Mock<IBackgroundTask> _backgroundTaskMock = new();
    Mock<IConfigEditor> _configEditorMock = new();
    Mock<INativeUserInterface> _nativeUserInterfaceMock = new();

    [Fact]
    public void Construct_WithDefaultServices()
    {
        new NotifyIconViewModel();
    }

    [Fact]
    public async Task Configure_CallsEditConfig()
    {
        _configEditorMock
            .Setup(m => m.EditConfig())
            .Returns(Task.CompletedTask)
            .Verifiable();

        var model = new NotifyIconViewModel(_appControlMock.Object, _autostartMock.Object, _backgroundTaskMock.Object, _configEditorMock.Object, _nativeUserInterfaceMock.Object);
        await model.ConfigureCommand.ExecuteAsync(null);

        _configEditorMock.Verify();
    }

    [Fact]
    public async Task IsConfigEditorEnabled_RespondsToEvents()
    {
        var model = new NotifyIconViewModel(_appControlMock.Object, _autostartMock.Object, _backgroundTaskMock.Object, _configEditorMock.Object, _nativeUserInterfaceMock.Object);

        Assert.True(model.IsConfigEditorEnabled);

        _configEditorMock.Setup(m => m.EditConfig())
            .Returns(Task.CompletedTask)
            .Raises(m => m.ConfigEditorOpened += null);
        await model.ConfigureCommand.ExecuteAsync(null);

        Assert.False(model.IsConfigEditorEnabled);

        _configEditorMock.Setup(m => m.EditConfig())
            .Returns(Task.CompletedTask)
            .Raises(m => m.ConfigEditorClosed += null);
        await model.Configure();

        Assert.True(model.IsConfigEditorEnabled);
    }

    [Fact]
    public void Exit_CallsShutdown()
    {
        _appControlMock.Setup(m => m.Shutdown())
            .Verifiable();

        var model = new NotifyIconViewModel(_appControlMock.Object, _autostartMock.Object, _backgroundTaskMock.Object, _configEditorMock.Object, _nativeUserInterfaceMock.Object);
        model.ExitCommand.Execute(null);

        _appControlMock.Verify();
    }

    [Fact]
    public void About_ShowsAboutBox()
    {
        _nativeUserInterfaceMock
            .Setup(m => m.ShowWindow<AboutWindow, AboutViewModel>(It.IsAny<AboutViewModel>(), It.IsAny<EventHandler>()))
            .Callback((AboutViewModel dataContext, EventHandler closedHandler) =>
            {
                // Simulate the window being closed
                closedHandler(null, EventArgs.Empty);
            })
            .Verifiable();

        var model = new NotifyIconViewModel(_appControlMock.Object, _autostartMock.Object, _backgroundTaskMock.Object, _configEditorMock.Object, _nativeUserInterfaceMock.Object);
        model.AboutCommand.Execute(null);

        _nativeUserInterfaceMock.Verify();
    }

    [Fact]
    public void IsAutostart_ControlsAutostart()
    {
        _autostartMock.Setup(m => m.SetEnabled(true));
        _autostartMock.Setup(m => m.SetEnabled(false));

        var model = new NotifyIconViewModel(_appControlMock.Object, _autostartMock.Object, _backgroundTaskMock.Object, _configEditorMock.Object, _nativeUserInterfaceMock.Object);

        Assert.False(model.IsAutostart);
        Assert.False(_autostartMock.Object.IsEnabled());

        model.IsAutostart = true;

        _autostartMock.Verify(m => m.SetEnabled(true), Times.Once);
        _autostartMock.Verify(m => m.SetEnabled(false), Times.Never);

        model.IsAutostart = false;

        _autostartMock.Verify(m => m.SetEnabled(true), Times.Once);
        _autostartMock.Verify(m => m.SetEnabled(false), Times.Once);
    }

    [Fact]
    public void EnableDisable_ControlsBackgroundTask()
    {
        _backgroundTaskMock.SetupSet(m => m.Enabled = false);
        _backgroundTaskMock.SetupSet(m => m.Enabled = true);

        var model = new NotifyIconViewModel(_appControlMock.Object, _autostartMock.Object, _backgroundTaskMock.Object, _configEditorMock.Object, _nativeUserInterfaceMock.Object);

        _backgroundTaskMock.SetupGet(m => m.Enabled).Returns(true);
        model.EnableDisableCommand.Execute(null);

        _backgroundTaskMock.VerifySet(m => m.Enabled = false, Times.Once);
        _backgroundTaskMock.VerifySet(m => m.Enabled = true, Times.Never);

        _backgroundTaskMock.SetupGet(m => m.Enabled).Returns(false);
        model.EnableDisableCommand.Execute(null);

        _backgroundTaskMock.VerifySet(m => m.Enabled = false, Times.Once);
        _backgroundTaskMock.VerifySet(m => m.Enabled = true, Times.Once);
    }

    [Fact]
    public void EnableDisable_UpdatesUI()
    {
        var model = new NotifyIconViewModel(_appControlMock.Object, _autostartMock.Object, _backgroundTaskMock.Object, _configEditorMock.Object, _nativeUserInterfaceMock.Object);

        var invocations = new Dictionary<string, int>();
        model.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName != null)
            {
                invocations[args.PropertyName] = invocations.GetValueOrDefault(args.PropertyName, 0) + 1;
            }
        };

        _backgroundTaskMock.SetupGet(m => m.Enabled).Returns(true);

        Assert.Equal("Disable", model.EnableDisableText);
        Assert.Equal("pack://application:,,,/UserInterface/TrayEnabled.ico", model.IconUri);

        model.EnableDisableCommand.Execute(null);

        _backgroundTaskMock.SetupGet(m => m.Enabled).Returns(false);

        Assert.Equal(1, invocations.GetValueOrDefault(nameof(model.EnableDisableText), 0));
        Assert.Equal("Enable", model.EnableDisableText);
        Assert.Equal(1, invocations.GetValueOrDefault(nameof(model.IconUri), 0));
        Assert.Equal("pack://application:,,,/UserInterface/TrayDisabled.ico", model.IconUri);

        model.EnableDisableCommand.Execute(null);

        _backgroundTaskMock.SetupGet(m => m.Enabled).Returns(true);

        Assert.Equal("Disable", model.EnableDisableText);
        Assert.Equal(2, invocations.GetValueOrDefault(nameof(model.EnableDisableText), 0));
        Assert.Equal("pack://application:,,,/UserInterface/TrayEnabled.ico", model.IconUri);
        Assert.Equal(2, invocations.GetValueOrDefault(nameof(model.IconUri), 0));
    }
}
