using System.IO.Abstractions.TestingHelpers;
using Moq;
using Serilog;
using Serilog.Core;

namespace ShadowKVM.Tests;

public class AppTests
{
    Mock<IAppControl> _appControlMock = new();
    Mock<IAutostart> _autostartMock = new();
    Mock<IBackgroundTask> _backgroundTaskMock = new();
    Mock<IConfigEditor> _configEditorMock = new();
    Mock<IConfigGenerator> _configGeneratorMock = new();
    Mock<IConfigService> _configServiceMock = new();
    Mock<INativeUserInterface> _nativeUserInterfaceMock = new();
    Mock<ILogger> _loggerMock = new();

    [Fact]
    public async Task OnStartupAsync_InitializesApplication()
    {
        var fileSystem = new MockFileSystem();

        var appBehavior = new AppBehavior("tEsTdAtDaTaDiReCtOrY", _appControlMock.Object, _autostartMock.Object,
            _backgroundTaskMock.Object, _configEditorMock.Object, _configGeneratorMock.Object, _configServiceMock.Object,
            fileSystem, _nativeUserInterfaceMock.Object, _loggerMock.Object, new LoggingLevelSwitch());

        await appBehavior.OnStartupAsync(new object(), EventArgs.Empty);

        Assert.True(fileSystem.Directory.Exists("tEsTdAtDaTaDiReCtOrY"));
    }

    [Fact]
    public void OnUnhandledException_LogsException()
    {
        var appBehavior = new AppBehavior("tEsTdAtDaTaDiReCtOrY", _appControlMock.Object, _autostartMock.Object,
            _backgroundTaskMock.Object, _configEditorMock.Object, _configGeneratorMock.Object, _configServiceMock.Object,
            new MockFileSystem(), _nativeUserInterfaceMock.Object, _loggerMock.Object, new LoggingLevelSwitch());

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
}
