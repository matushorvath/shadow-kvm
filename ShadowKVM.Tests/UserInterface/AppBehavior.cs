using System.Reflection;
using Moq;
using Serilog;

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
    public void OnUnhandledException_LogsException()
    {
        var appBehavior = new AppBehavior(
            "tEsTdAtDaTaDiReCtOrY",
            _appControlMock.Object,
            _autostartMock.Object,
            _backgroundTaskMock.Object,
            _configEditorMock.Object,
            _configGeneratorMock.Object,
            _configServiceMock.Object,
            _nativeUserInterfaceMock.Object,
            _loggerMock.Object,
            new()
        );

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
