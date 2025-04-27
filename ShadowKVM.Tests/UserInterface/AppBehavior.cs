using System;
using System.IO.Abstractions.TestingHelpers;
using System.Windows;
using Moq;
using Serilog;
using Serilog.Core;

namespace ShadowKVM.Tests;

public class AppBehaviorTests
{
    Mock<IAppControl> _appControlMock = new();
    Mock<IAutostart> _autostartMock = new();
    Mock<IBackgroundTask> _backgroundTaskMock = new();
    Mock<IConfigEditor> _configEditorMock = new();
    Mock<IConfigGenerator> _configGeneratorMock = new();
    Mock<IConfigService> _configServiceMock = new();
    Mock<INativeUserInterface> _nativeUserInterfaceMock = new();
    Mock<ILogger> _loggerMock = new();

    MockFileSystem _fileSystem = new();
    LoggingLevelSwitch _loggingLevelSwitch = new();

    AppBehavior CreateAppBehavior()
    {
        return new AppBehavior("tEsTdAtDaTaDiReCtOrY", _appControlMock.Object, _autostartMock.Object,
            _backgroundTaskMock.Object, _configEditorMock.Object, _configGeneratorMock.Object, _configServiceMock.Object,
            _fileSystem, _nativeUserInterfaceMock.Object, _loggerMock.Object, _loggingLevelSwitch);
    }

    [Fact]
    public async Task OnStartupAsync_LogsStartup()
    {
        var appBehavior = CreateAppBehavior();
        await appBehavior.OnStartupAsync(new object(), EventArgs.Empty);

        _loggerMock.Verify(logger => logger.Information("Initializing, version {FullSemVer} ({CommitDate})",
            It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _loggerMock.Verify(logger => logger.Debug("Version: {InformationalVersion}",
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task OnStartupAsync_CreatesDataDirectory()
    {
        var appBehavior = CreateAppBehavior();
        await appBehavior.OnStartupAsync(new object(), EventArgs.Empty);

        Assert.True(_fileSystem.Directory.Exists("tEsTdAtDaTaDiReCtOrY"));
    }

    [Fact]
    public async Task OnStartupAsync_EnablesAutostartIfNotConfigured()
    {
        _autostartMock.Setup(autostart => autostart.IsConfigured())
            .Returns(false)
            .Verifiable();

        var appBehavior = CreateAppBehavior();
        await appBehavior.OnStartupAsync(new object(), EventArgs.Empty);

        _autostartMock.Verify(autostart => autostart.SetEnabled(true), Times.Once);
        _autostartMock.Verify();
    }

    [Fact]
    public async Task OnStartupAsync_SkipsAutostartIfConfigured()
    {
        _autostartMock
            .Setup(autostart => autostart.IsConfigured())
            .Returns(true)
            .Verifiable();

        var appBehavior = CreateAppBehavior();
        await appBehavior.OnStartupAsync(new object(), EventArgs.Empty);

        _autostartMock.Verify(autostart => autostart.SetEnabled(It.IsAny<bool>()), Times.Never);
        _autostartMock.Verify();
    }

    [Fact]
    public async Task OnStartupAsync_InitConfig_InitsConfigService()
    {
        var appBehavior = CreateAppBehavior();
        await appBehavior.OnStartupAsync(new object(), EventArgs.Empty);

        _configServiceMock.Verify(configService => configService.SetDataDirectory("tEsTdAtDaTaDiReCtOrY"), Times.Once);
        _configServiceMock.VerifyAdd(configService => configService.ConfigChanged += It.IsAny<Action<IConfigService>>(), Times.Once);
    }

    [Fact]
    public async Task OnStartupAsync_InitConfig_Loads()
    {
        _configServiceMock
            .Setup(configService => configService.ReloadConfig())
            .Verifiable();

        var appBehavior = CreateAppBehavior();
        await appBehavior.OnStartupAsync(new object(), EventArgs.Empty);

        _configServiceMock.Verify();

        // Verify that no questions were asked
        _nativeUserInterfaceMock.Verify(nativeUserInterface => nativeUserInterface.QuestionBox(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OnStartupAsync_InitConfig_NotFound_Create()
    {
        _configServiceMock
            .Setup(configService => configService.ReloadConfig())
            .Throws(new FileNotFoundException())
            .Verifiable();

        _nativeUserInterfaceMock
            .Setup(nativeUserInterface => nativeUserInterface.QuestionBox(
                "Configuration file not found, create a new one?", "Shadow KVM"))
            .Returns(true)
            .Verifiable();

        _nativeUserInterfaceMock
            .Setup(nativeUserInterface => nativeUserInterface.ShowWindow(It.IsAny<Action<ConfigGeneratorWindow>>()))
            .Verifiable();

        _configGeneratorMock
            .Setup(configGenerator => configGenerator.Generate(It.IsAny<IProgress<ConfigGeneratorStatus>>()))
            .Returns("cOnFiGfIlEcOnTeNt")
            .Verifiable();

        _configServiceMock
            .SetupGet(configService => configService.ConfigPath)
            .Returns(@"C:\cOnFiGpAtH")
            .Verifiable();

        var appBehavior = CreateAppBehavior();
        await appBehavior.OnStartupAsync(new object(), EventArgs.Empty);

        _configServiceMock.Verify();

        // Don't shutdown, since we want to create a new config
        _appControlMock.Verify(appControl => appControl.Shutdown(), Times.Never);

        // Verify that the config file was created
        Assert.Equal("cOnFiGfIlEcOnTeNt", await _fileSystem.File.ReadAllTextAsync(@"C:\cOnFiGpAtH"));
    }

    // TODO test restart when config file changed

    [Fact]
    public void OnUnhandledException_LogsException()
    {
        var appBehavior = CreateAppBehavior();
        appBehavior.OnUnhandledException(new(), new UnhandledExceptionEventArgs(new Exception("tEsTeXcEpTiOn"), true));

        var uiMessage = """
            Shadow KVM encountered an error and needs to close.
            
            tEsTeXcEpTiOn
            
            See tEsTdAtDaTaDiReCtOrY\logs for details.
            """;
        _nativeUserInterfaceMock.Verify(ui => ui.ErrorBox(uiMessage, "Shadow KVM"), Times.Once);

        _loggerMock.Verify(logger => logger.Error("Unhandled exception: {@Exception}",
            It.Is<object[]>(p => p.Length == 1 && ((Exception)p[0]).Message == "tEsTeXcEpTiOn")), Times.Once);
    }

    [Fact]
    public void OnUnhandledException_LogsNonException()
    {
        var appBehavior = CreateAppBehavior();
        appBehavior.OnUnhandledException(new(), new UnhandledExceptionEventArgs("tEsTeXcEpTiOn", true));

        var uiMessage = """
            Shadow KVM encountered an error and needs to close.
            
            tEsTeXcEpTiOn
            
            See tEsTdAtDaTaDiReCtOrY\logs for details.
            """;
        _nativeUserInterfaceMock.Verify(ui => ui.ErrorBox(uiMessage, "Shadow KVM"), Times.Once);

        _loggerMock.Verify(logger => logger.Error("Unhandled exception: {@Exception}",
            It.Is<object[]>(p => p.Length == 1 && (string)p[0] == "tEsTeXcEpTiOn")), Times.Once);
    }

    [Fact]
    public void OnUnobservedTaskException_LogsException()
    {
        var appBehavior = CreateAppBehavior();
        appBehavior.OnUnobservedTaskException(null, new UnobservedTaskExceptionEventArgs(new AggregateException("tEsTeXcEpTiOn")));

        var uiMessage = """
            Shadow KVM encountered an error and needs to close.
            
            tEsTeXcEpTiOn
            
            See tEsTdAtDaTaDiReCtOrY\logs for details.
            """;
        _nativeUserInterfaceMock.Verify(ui => ui.ErrorBox(uiMessage, "Shadow KVM"), Times.Once);

        _loggerMock.Verify(logger => logger.Error("Unobserved task exception: {@Exception}",
            It.Is<AggregateException>(e => e.Message == "tEsTeXcEpTiOn")), Times.Once);
    }
}
