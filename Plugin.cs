using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Game.Config;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

namespace Upscalingway;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IGameConfig GameConfig { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;

    public Plugin()
    {
        Log.Information("[Upscalingway] Plugin loading...");

        ClientState.Login += OnLogin;
        AddonLifecycle.RegisterListener(AddonEvent.PostSetup, new[] { "_TitleMenu", "_CharaSelect" }, OnEarlyMenu);
        Framework.Update += OnFrameworkUpdate;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        // If the game is idle and we are not logged in, we are likely at a menu.
        // We want FSR in menus because DLAA is forced no matter what.
        if (!ClientState.IsLoggedIn && ClientState.IsClientIdle())
        {
            if (GameConfig.TryGet(SystemConfigOption.GraphicsRezoUpscaleType, out uint upscaleType) && upscaleType != 0)
            {
                Log.Information("[Upscalingway] Game is idle at menu, ensuring FSR is active.");
                SetUpscaleType(0u);
            }
            // We can stop checking once we are sure we handled the menu state, 
            // but we'll keep it active to handle transitions back to menu if they happen.
        }
    }

    private void OnEarlyMenu(AddonEvent type, AddonArgs args)
    {
        Log.Information($"[Upscalingway] Menu detected, switching to FSR.");
        SetUpscaleType(0u);
    }

    private void OnLogin()
    {
        Log.Information("[Upscalingway] Login detected, switching to DLSS.");
        // Switch to FSR then back to DLSS to ensure it's unstuck and using DLSSTweaker params
        SetUpscaleType(0u);
        SetUpscaleType(1u);
    }

    private void SetUpscaleType(uint type)
    {
        try
        {
            GameConfig.Set(SystemConfigOption.GraphicsRezoUpscaleType, type);
        }
        catch (System.Exception ex)
        {
            Log.Error(ex, $"[Upscalingway] Error while trying to set upscale type to {type}.");
        }
    }

    public void Dispose()
    {
        ClientState.Login -= OnLogin;
        AddonLifecycle.UnregisterListener(OnEarlyMenu);
        Framework.Update -= OnFrameworkUpdate;
    }
}
