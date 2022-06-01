using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace QuitToDesktop
{
    [BepInPlugin(PluginId, DisplayName, Version)]
    public class QuitToDesktop : BaseUnityPlugin
    {
        public const string PluginId = "randyknapp.mods.quitotdesktop";
        public const string DisplayName = "Quit to Desktop";
        public const string Version = "1.0.0";

        private static ConfigEntry<bool> _loggingEnabled;

        private static QuitToDesktop _instance;
        private Harmony _harmony;

        private void Awake()
        {
            _loggingEnabled = Config.Bind("Logging", "LoggingEnabled", true, "Enable logging");
            _instance = this;
            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginId);

            Log("QuitToDesktop Awake");
        }

        public static void Log(string message)
        {
            if (_loggingEnabled.Value)
            {
                _instance.Logger.LogInfo(message);
            }
        }

        public static void LogWarning(string message)
        {
            if (_loggingEnabled.Value)
            {
                _instance.Logger.LogWarning(message);
            }
        }

        public static void LogError(string message)
        {
            if (_loggingEnabled.Value)
            {
                _instance.Logger.LogError(message);
            }
        }
    }
}
