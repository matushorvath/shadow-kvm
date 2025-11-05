using System.IO;
using Serilog;

namespace ShadowKVM;

public interface IConfigEditor
{
    event Action? ConfigEditorOpened;
    event Action? ConfigEditorClosed;

    Task EditConfig();
}

public class ConfigEditor(IConfigService configService, IAppControl appControl, INativeUserInterface nativeUserInterface) : IConfigEditor
{
    public event Action? ConfigEditorOpened;
    public event Action? ConfigEditorClosed;

    enum EditorResult { Succeeded, FailedRetry, FailedAbort };

    public async Task EditConfig()
    {
        ConfigEditorOpened?.Invoke();

        try
        {
            while (true)
            {
                var result = await OpenEditor();
                switch (result)
                {
                    case EditorResult.Succeeded:
                        // The config file was edited and succesfully loaded
                        return;
                    case EditorResult.FailedRetry:
                        // Loading the edited config file failed and user chose to retry
                        break;
                    case EditorResult.FailedAbort:
                        // Loading the edited config file failed and user chose to abort
                        appControl.Shutdown();
                        return;
                }
            }
        }
        finally
        {
            ConfigEditorClosed?.Invoke();
        }
    }

    async Task<EditorResult> OpenEditor()
    {
        // Keep opening the editor until the config file loads or until user gives up
        {
            try
            {
                // Open notepad to edit the config file and wait for it to close
                await nativeUserInterface.OpenEditor(configService.ConfigPath);
                configService.ReloadConfig();

                nativeUserInterface.InfoBox("Configuration file loaded successfully", "Shadow KVM");

                return EditorResult.Succeeded;
            }
            catch (ConfigException exception)
            {
                var message = $"""
                    Configuration file could not be loaded, retry editing?

                    {exception.Message}
                    """;

                var retry = nativeUserInterface.QuestionBox(message.ReplaceLineEndings(), "Shadow KVM");
                return retry ? EditorResult.FailedRetry : EditorResult.FailedAbort;
            }
        }
    }
}
