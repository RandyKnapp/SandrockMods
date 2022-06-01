using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using JetBrains.Annotations;
using Pathea;
using Pathea.InputSolution;
using Pathea.UISystemV2.UI;
using TMPro;
using UnityEngine;

namespace CursorLockOption
{
    [BepInPlugin(PluginId, DisplayName, Version)]
    public class CursorLockOption : BaseUnityPlugin
    {
        public const string PluginId = "randyknapp.mods.cursorlockoption";
        public const string DisplayName = "Cursor Lock Option";
        public const string Version = "1.0.0";

        private static ConfigEntry<bool> _loggingEnabled;
        private static ConfigEntry<bool> _cursorLocked;
        private static CursorLockOption _instance;
        private static MethodInfo _calculateMouseState;
        private static ChoiceElement _cursorLockedChoiceElement = null;
        private static OptionChoiceElement _cursorLockedOptionChoiceElement = null;

        private Harmony _harmony;

        [UsedImplicitly]
        public void Awake()
        {
            _calculateMouseState = AccessTools.Method(typeof(UISystemMgr.UISystemMouseMgr), "CalculateMouseState");
            _instance = this;

            _loggingEnabled = Config.Bind("Logging", "LoggingEnabled", true, "Enable logging");
            _cursorLocked = Config.Bind("Cursor", "CursorLockedToScreen", false, "Set the cursor locked to the screen or not");
            _cursorLocked.SettingChanged += OnSettingChanged;

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

        private static void OnSettingChanged(object sender, EventArgs e)
        {
            var uiSystemMgr = GameObject.FindObjectOfType<UISystemMgr>();
            if (uiSystemMgr != null && _calculateMouseState != null)
            {
                _calculateMouseState.Invoke(uiSystemMgr.UIMouseMgr, new object[] { });
            }
        }

        [HarmonyPatch(typeof(UISystemMgr.UISystemMouseMgr), "CalculateMouseState")]
        public static class UISystemMouseMgr_CalculateMouseState_Patch
        {
            [UsedImplicitly]
            private static bool Prefix(List<object> ___forceHide, List<object> ___showList, bool ___onlyHideMouseView)
            {
                var defaultMouseState = _cursorLocked.Value ? CursorLockMode.Confined : CursorLockMode.None;
                var notUsingMouse = InputModule.Self.Mgr.DeviceDetector.CurDevice > InputDevice.MouseKeyboard;
                if (___forceHide.Count > 0)
                {
                    Cursor.lockState = notUsingMouse ? defaultMouseState : CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                else if (___showList.Count > 0)
                {
                    Cursor.lockState = defaultMouseState;
                    Cursor.visible = !___onlyHideMouseView;
                }
                else
                {
                    Cursor.lockState = notUsingMouse ? defaultMouseState : CursorLockMode.Locked;
                    Cursor.visible = false;
                }

                return false;
            }
        }

        //OptionPartUI.StartEdit
        [HarmonyPatch(typeof(OptionPartUI), "StartEdit")]
        public static class OptionPartUI_StartEdit_Patch
        {
            [UsedImplicitly]
            private static void Postfix(OptionPartUI __instance)
            {
                if (_cursorLockedChoiceElement == null)
                {
                    var original = __instance.crashReportChoice.transform.parent.parent.GetComponent<ChoiceElement>();
                    _cursorLockedChoiceElement = Instantiate(original, original.transform.parent);
                    _cursorLockedChoiceElement.transform.SetSiblingIndex(original.transform.GetSiblingIndex() + 1);
                    _cursorLockedChoiceElement.gameObject.name = "CursorLock";

                    _cursorLockedOptionChoiceElement = _cursorLockedChoiceElement.GetComponentInChildren<OptionChoiceElement>();
                    AccessTools.FieldRefAccess<ChoiceElement, OptionChoiceElement>(_cursorLockedChoiceElement, "OptionChoiceElement") = _cursorLockedOptionChoiceElement;
                    _cursorLockedOptionChoiceElement.SetOnOffTranslation();
                    _cursorLockedOptionChoiceElement.SetIndex(_cursorLocked.Value ? 1 : 0);
                    _cursorLockedOptionChoiceElement.OnChange = null;
                    _cursorLockedOptionChoiceElement.OnChange += OnOptionChanged;

                    // I'm not really sure why this isn't setting the title property, so wait a frame and set it
                    CoroutineMgr.Instance.StartCoroutine(SetTitleCoroutine());
                }

                if (_cursorLockedOptionChoiceElement != null)
                {
                    _cursorLockedOptionChoiceElement.SetIndex(_cursorLocked.Value ? 1 : 0);
                }
            }

            private static IEnumerator SetTitleCoroutine()
            {
                yield return null;

                var title = _cursorLockedChoiceElement.GetComponentInChildren<TextMeshProUGUI>();
                title.text = "Lock Cursor to Screen";
            }

            private static void OnOptionChanged(int index)
            {
                var optionIsOn = _cursorLockedOptionChoiceElement.GetValue() != 0;
                _cursorLocked.Value = optionIsOn;
            }
        }
    }
}
