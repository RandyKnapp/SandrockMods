using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using JetBrains.Annotations;
using Pathea;
using Pathea.ArchiveNs;
using Pathea.InfoTip;
using Pathea.SaveNs;
using Pathea.UISystemV2.Grid;
using Pathea.UISystemV2.UI;
using Pathea.UISystemV2.UIControl;
using UnityEngine;

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
        private int _justQuitIndex = -1;
        private int _saveAndQuitIndex = -1;

        [UsedImplicitly]
        private void Awake()
        {
            _loggingEnabled = Config.Bind("Logging", "LoggingEnabled", true, "Enable logging");
            _instance = this;
            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginId);
        }

        [UsedImplicitly]
        private void OnDestroy()
        {
            _harmony.UnpatchSelf();
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

        [HarmonyPatch(typeof(EscMenuUI), nameof(EscMenuUI.InitMenu))]
        public static class EscMenuUI_InitMenu_Patch
        {
            [UsedImplicitly]
            public static bool Prefix(EscMenuUI __instance, ref string[] menuName)
            {
                _instance._justQuitIndex = menuName.Length;
                _instance._saveAndQuitIndex = _instance._justQuitIndex + 1;
                menuName = menuName.AddRangeToArray(new []{ "Quit to Desktop", "Save & Quit to Desktop" });
                return true;
            }

            [UsedImplicitly]
            public static void Postfix(EscMenuUI __instance)
            {
                if (_instance._saveAndQuitIndex >= 0 && __instance.buttons.Count > _instance._saveAndQuitIndex)
                {
                    __instance.buttons[_instance._saveAndQuitIndex].GetComponent<GridEventHandler_NoDrag>().interactable = Save.Self.CanSave();
                }
            }
        }

        //IUIPartControl IEscMenuControl.SelectMenu(int index)
        [HarmonyPatch(typeof(EscMenuControl), "Pathea.UISystemV2.UI.IEscMenuControl.SelectMenu")]
        public static class EscMenuControl_SelectMenu_Patch
        {
            private static EscMenuControl _escMenuControl;

            [UsedImplicitly]
            public static bool Prefix(EscMenuControl __instance, ref IUIPartControl __result, int index, bool ___isExiting)
            {
                if (_instance == null || _instance._saveAndQuitIndex < 0)
                    return true;

                _escMenuControl = __instance;
                if (index == _instance._justQuitIndex)
                {
                    __result = null;
                    ChoiceTipModule.Instance.StartChoiceTip("Quit to Desktop? Unsaved progress will be lost.", ConfirmQuitToDesktop, null);
                    InputModule.Self.VirtualMouse.RemoveEnable(_escMenuControl);
                    __result = null;
                    return false;
                }
                else if (index == _instance._saveAndQuitIndex)
                {
                    if (!Save.Self.CanSave())
                        return true;

                    InputModule.Self.Mgr.AddDisableInputIfNotExist(__instance);
                    ArchiveMgr.Self.OnSaveSuccessful += OnSaveSuccessful;
                    Save.Self.RequestAuto();
                    InputModule.Self.VirtualMouse.RemoveEnable(_escMenuControl);
                    __result = null;
                    return false;
                }

                return true;
            }

            private static void ConfirmQuitToDesktop()
            {
                Application.Quit();
            }

            private static void OnSaveSuccessful(bool success, int tip)
            {
                ArchiveMgr.Self.OnSaveSuccessful -= OnSaveSuccessful;
                if (success)
                {
                    Application.Quit();
                }
                else
                {
                    InputModule.Self.Mgr.RemoveDisableInput(_escMenuControl);
                    InfoTipMgr.Self.SendSimpleTip(TextMgr.GetStr(tip));
                }
            }
        }

        //protected override void OnUIRelease()
        [HarmonyPatch(typeof(EscMenuControl), "OnUIRelease")]
        public static class EscMenuControl_OnUIRelease_Patch
        {
            [UsedImplicitly]
            public static void Postfix()
            {
                if (_instance != null)
                {
                    _instance._justQuitIndex = -1;
                    _instance._saveAndQuitIndex = -1;
                }
            }
        }
    }
}
