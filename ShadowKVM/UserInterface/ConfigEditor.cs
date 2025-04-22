using System.Diagnostics;
using Serilog;

namespace ShadowKVM;

public interface IConfigEditor
{
    event Action? ConfigEditorOpened;
    event Action? ConfigEditorClosed;

    Task EditConfig();
}

public class ConfigEditor(IConfigService configService, INativeUserInterface nativeUserInterface, ILogger logger) : IConfigEditor
{
    public event Action? ConfigEditorOpened;
    public event Action? ConfigEditorClosed;

    public async Task EditConfig()
    {
        ConfigEditorOpened?.Invoke();

        try
        {
            // Open notepad to edit the config file and wait for it to close
            await nativeUserInterface.OpenEditor(configService.ConfigPath);

            bool reloaded = configService.ReloadConfig();
            if (!reloaded)
            {
                logger.Information("Configuration file has not changed, skipping reload");
                return;
            }

            nativeUserInterface.InfoBox("Configuration file loaded successfully", "Shadow KVM");
        }
        finally
        {
            ConfigEditorClosed?.Invoke();
        }
    }
}
