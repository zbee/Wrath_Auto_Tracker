using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.IoC;
using ECommons;
using WrathIcon.Core;
using WrathIcon.Utilities;

namespace WrathIcon
{
    public sealed class Plugin : IDalamudPlugin
    {
        private const string CommandName = "/wrathicon";
        public readonly WindowSystem WindowSystem = new("Wrath Status Icon");

        private MainWindow mainWindow = null!;
        private ConfigWindow configWindow = null!;
        private Configuration config = null!;
        private TextureManager textureManager = null!;
        public static bool IsWrathEnabled => WrathIPC.AutoRotationState;

        private bool isInitialized = false;

        [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
        [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService] internal static IPluginLog PluginLog { get; private set; } = null!;
        [PluginService] internal static IFramework Framework { get; private set; } = null!;
        [PluginService] internal static IClientState ClientState { get; private set; } = null!;

        public string Name => "Wrath Status Icon";

        public Plugin(IDalamudPluginInterface pluginInterface)
        {
            ECommonsMain.Init(pluginInterface, this);
            Framework.Update += OnFrameworkUpdate;

            ClientState.Login += OnLogin;
            ClientState.Logout += OnLogout;

            ClientState.TerritoryChanged += OnTerritoryChanged;
        }

        private void OnFrameworkUpdate(IFramework framework)
        {
            if (ClientState.IsLoggedIn && !isInitialized)
            {
                Initialize();
                isInitialized = true;
                Framework.Update -= OnFrameworkUpdate;
            }
        }

        private void Initialize()
        {
            config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            config.Initialize(PluginInterface);

            textureManager = new TextureManager(TextureProvider);

            PluginLog.Information("[Debug] Plugin initializing...");

            mainWindow = new MainWindow(config, wrathStateManager, textureManager)

            {
                IsOpen = ClientState.IsLoggedIn 
            };
            configWindow = new ConfigWindow(config);

            RegisterWindow(mainWindow);
            RegisterWindow(configWindow);

            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Toggle Wrath Status Icon UI"
            });

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += OpenConfigWindow;
            PluginInterface.UiBuilder.OpenMainUi += OpenMainWindow;

            PluginLog.Information("Plugin initialized.");
        }

        private void OnTerritoryChanged(ushort territoryId)
        {
            PluginLog.Debug($"Territory changed to: {territoryId}");

            if (!mainWindow.IsOpen)
            {
                PluginLog.Debug("Reopening MainWindow due to territory change.");
                mainWindow.IsOpen = true;
            }
        }

        private void RegisterWindow(Window window)
        {
            PluginLog.Debug($"Registering window: {window.WindowName}");
            WindowSystem.AddWindow(window);
        }

        private void OnLogin()
        {
            PluginLog.Debug("Login detected.");

            mainWindow.IsOpen = true;
            PluginLog.Debug("MainWindow shown due to login.");
        }

        private void OnLogout(int type, int code)
        {
            PluginLog.Debug($"Logout detected. Type: {type}, Code: {code}");

            mainWindow.IsOpen = false;
            PluginLog.Debug("MainWindow hidden due to logout.");
        }

        private void OnCommand(string command, string args)
        {
            PluginLog.Debug("Command triggered to toggle ConfigWindow.");
            configWindow.Toggle();
        }

        private void OpenConfigWindow()
        {
            if (!configWindow.IsOpen)
                configWindow.Toggle();
        }

        private void OpenMainWindow()
        {
            if (!mainWindow.IsOpen)
                mainWindow.Toggle();
        }

        private void DrawUI()
        {
            WindowSystem.Draw();
        }

        public void Dispose()
        {
            ECommonsMain.Dispose();

            PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigWindow;
            PluginInterface.UiBuilder.OpenMainUi -= OpenMainWindow;

            ClientState.Login -= OnLogin;
            ClientState.Logout -= OnLogout;
            ClientState.TerritoryChanged -= OnTerritoryChanged;

            CommandManager.RemoveHandler(CommandName);
            PluginInterface.UiBuilder.Draw -= DrawUI;
            WindowSystem.RemoveAllWindows();
        }
    }
}
