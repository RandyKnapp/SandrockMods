using System.Globalization;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Pathea.ActorNs;
using Pathea.FrameworkNs;
using Pathea.UISystemV2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

namespace ShowMoneyInHUD
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class ShowMoneyInHUD : BaseUnityPlugin
    {
        public const string GUID = "randyknapp.mods.showmoneyinhud";
        public const string NAME = "Show Money In HUD";
        public const string VERSION = "0.1.0";

        public static ManualLogSource Log;

        public void Awake()
        {
            Log = Logger;

            Logger.LogInfo($"{NAME} Loaded");
            //Logger.LogWarning($"- {Config.Definition.Key}: {Config.Value}");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), GUID);
        }
    }

    [HarmonyPatch]
    public static class GamingUIPatch
    {
        public static Transform GoldGroup;
        public static TextMeshProUGUI GoldText;
        public static Sprite GoldSprite;

        [HarmonyPatch(typeof(GamingUI), "ViewEnable")]
        [HarmonyPostfix]
        public static void ViewEnable_Postfix(GamingUI __instance)
        {
            Module<Player>.Self.bag.OnGoldChange += UpdateGoldInHUD;

            var foundGoldGroup = __instance.transform.Find("RightBottom/MiniMapRoot/TimeUI/GoldGroup");
            if (foundGoldGroup != null)
            {
                GoldGroup = foundGoldGroup;
                GoldText = GoldGroup.GetComponentInChildren<TextMeshProUGUI>();
                UpdateGoldInHUD();
                return;
            }

            if (GoldGroup == null)
            {
                var timeGroup = __instance.transform.Find("RightBottom/MiniMapRoot/TimeUI/TimeGroup");
                if (!timeGroup)
                {
                    ShowMoneyInHUD.Log.LogWarning("Did not find TimeGroup");
                    return;
                }

                GoldGroup = Object.Instantiate(timeGroup, timeGroup.parent);
                GoldGroup.gameObject.name = "GoldGroup";
                GoldText = GoldGroup.GetComponentInChildren<TextMeshProUGUI>();
                UpdateGoldInHUD();

                var rt = (RectTransform)GoldGroup;
                rt.anchoredPosition = new Vector2(-279, 189);

                var icon = GoldGroup.Find("Weather/Weather_icon").GetComponent<Image>();
                var iconRT = (RectTransform)icon.transform;
                iconRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 40);
                iconRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 40);
                if (GoldSprite == null)
                {
                    var allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
                    foreach (var sprite in allSprites)
                    {
                        if (sprite.name == "I_Backpack_icon_Coin_00")
                        {
                            GoldSprite = sprite;
                            icon.sprite = sprite;
                            break;
                        }
                    }
                }
                else
                {
                    icon.sprite = GoldSprite;
                }

                var pauseTime = GoldGroup.Find("PauseTime");
                if (pauseTime != null)
                    Object.Destroy(pauseTime.gameObject);
            }
        }

        [HarmonyPatch(typeof(GamingUI), "ViewDisable")]
        [HarmonyPostfix]
        public static void ViewDisable_Postfix(GamingUI __instance)
        {
            Module<Player>.Self.bag.OnGoldChange -= UpdateGoldInHUD;
            GoldGroup = null;
            GoldText = null;
        }

        public static void UpdateGoldInHUD()
        {
            if (GoldText != null)
                GoldText.text = Module<Player>.Self.bag.Gold.ToString("n0", new CultureInfo("en-US"));
        }
    }
}
