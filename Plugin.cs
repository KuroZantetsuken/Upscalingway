using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace Upscalingway;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IGameConfig GameConfig { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private bool _hasRun;

    public Plugin()
    {
        ClientState.Login += OnLogin;

        if (ClientState.IsLoggedIn)
        {
            ApplyFix();
        }
    }

    private void OnLogin()
    {
        ApplyFix();
    }

    private void ApplyFix()
    {
        if (_hasRun) return;
        _hasRun = true;

        try
        {
            if (GameConfig.System.TryGetUInt("GraphicsRezoUpscaleType", out var upscaleType))
            {
                // Only run the fix if they actually have DLSS (1) enabled
                if (upscaleType == 1)
                {
                    Log.Information($"[Upscalingway] Applying startup DLSS toggle to force DLSSTweaker parameters...");
                    GameConfig.System.Set("GraphicsRezoUpscaleType", 0u); // Switch to FSR
                    GameConfig.System.Set("GraphicsRezoUpscaleType", 1u); // Switch back to DLSS
                    Log.Information($"[Upscalingway] DLSS toggle complete.");
                }
            }
        }
        catch (System.Exception ex)
        {
            Log.Error(ex, "[Upscalingway] Error while trying to fix DLSS parameters.");
        }
    }

    public void Dispose()
    {
        ClientState.Login -= OnLogin;
    }
}