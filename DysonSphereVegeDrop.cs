//
// Copyright (c) 2021, Aaron Shumate
// All rights reserved.
//
// This source code is licensed under the BSD-style license found in the
// LICENSE.txt file in the root directory of this source tree. 
//
// Dyson Sphere Program is developed by Youthcat Studio and published by Gamera Game.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using BepInEx.Logging;
using System.Security;
using System.Reflection;
using System.Security.Permissions;
using NGPT;
using System.Collections;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
namespace DysonSphereVegeDrop
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    [BepInProcess("DSPGAME.exe")]
    [BepInProcess("Dyson Sphere Program.exe")]
    public class DysonSphereVegeDrop : BaseUnityPlugin
    {
        public const string pluginGuid = "w-gates.dysonsphereprogram.vegedrop";
        public const string pluginName = "DSP Vege Drop";
        public const string pluginVersion = "1.0.0";
        new internal static ManualLogSource Logger;
        new internal static BepInEx.Configuration.ConfigFile Config;
        Harmony harmony;

        public static BepInEx.Configuration.ConfigEntry<bool> configEnableMod;
        public static BepInEx.Configuration.ConfigEntry<bool> configEnableDebug;

        public static BepInEx.Configuration.ConfigEntry<bool> configEnableClearingItemTree;
        public static BepInEx.Configuration.ConfigEntry<bool> configEnableClearingItemStone;
        public static BepInEx.Configuration.ConfigEntry<bool> configEnableClearingItemDetail;
        public static BepInEx.Configuration.ConfigEntry<bool> configEnableClearingItemIce;
        public static BepInEx.Configuration.ConfigEntry<bool> configEnableClearingItemSpaceCapsule;
        public static BepInEx.Configuration.ConfigEntry<string> configDisableClearingItemIds_StringConfigEntry;
        public static short[] configDisableClearingItemIds_ShortArray;

        public static BepInEx.Configuration.ConfigEntry<bool> configEnableClearingPlanetAridDesert;
        public static BepInEx.Configuration.ConfigEntry<bool> configEnableClearingPlanetAshenGelisol;
        public static BepInEx.Configuration.ConfigEntry<bool> configEnableClearingPlanetBarrenDesert;
        public static BepInEx.Configuration.ConfigEntry<bool> configEnableClearingPlanetGobi;
        public static BepInEx.Configuration.ConfigEntry<bool> configEnableClearingPlanetIceFieldGelisol;
        public static BepInEx.Configuration.ConfigEntry<bool> configEnableClearingPlanetLava;
        public static BepInEx.Configuration.ConfigEntry<bool> configEnableClearingPlanetMediterranean;
        public static BepInEx.Configuration.ConfigEntry<bool> configEnableClearingPlanetOceanWorld;
        public static BepInEx.Configuration.ConfigEntry<bool> configEnableClearingPlanetOceanicJungle;
        public static BepInEx.Configuration.ConfigEntry<bool> configEnableClearingPlanetPrairie;
        public static BepInEx.Configuration.ConfigEntry<bool> configEnableClearingPlanetRedStone;
        public static BepInEx.Configuration.ConfigEntry<bool> configEnableClearingPlanetVolcanicAsh;
        public static Dictionary<String, String> planetTypeConfigMap;
        public static Dictionary<String, String> configPlanetTypeMap;
        public static HashSet<String> disabledPlanets = new HashSet<String>();

        public static BepInEx.Configuration.ConfigEntry<Color> configIconColor_enabled;
        public static BepInEx.Configuration.ConfigEntry<Color> configIconColor_disabled;
        public static BepInEx.Configuration.ConfigEntry<Color> configIconColor_planet;


        public void Awake()
        {
            Logger = base.Logger;  // "C:\Program Files (x86)\Steam\steamapps\common\Dyson Sphere Program\BepInEx\LogOutput.log"
            Config = base.Config;
            Logger.LogDebug("Awake.");

            try
            {
                planetTypeConfigMap = new Dictionary<String, String>()
                {
                    { "干旱荒漠", "IncludeAridDesert" },
                    { "灰烬冻土", "IncludeAshenGelisol" },
                    { "贫瘠荒漠", "IncludeBarrenDesert" },
                    { "戈壁", "IncludeGobi" },
                    { "冰原冻土", "IncludeIceFieldGelisol" },
                    { "熔岩", "IncludeLava" },
                    { "地中海", "IncludeMediterranean" },
                    { "水世界", "IncludeOceanWorld" },
                    { "海洋丛林", "IncludeOceanicJungle" },
                    { "草原", "IncludePrairie" },
                    { "红石", "IncludeRedStone" },
                    { "火山灰", "IncludeVolcanicAsh" }
                };
                configPlanetTypeMap = planetTypeConfigMap.ToDictionary((i) => i.Value, (i) => i.Key);
                Logger.LogDebug($"Awake: configPlanetTypeMap - {String.Join(",", configPlanetTypeMap.Select(x => "[" + x.Key + ": " + x.Value + "]"))}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Awake - Unable to create maps: {ex.Message}");
                throw ex;
            }

            try
            {
                InitialConfigSetup();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Awake - Unable to set up config: {ex.Message}");
                throw ex;
            }

            try
            {
                harmony = new Harmony(pluginGuid);
                harmony.PatchAll(typeof(DysonSphereVegeDrop));
            }
            catch (Exception ex)
            {
                Logger.LogError($"Awake - Unable to patch: {ex.Message}");
                throw ex;
            }

            Logger.LogInfo("Awake - Initialization complete.");
        }

        public enum Substate { NORMAL, PLANET };
        public static Substate enableSubstate = Substate.NORMAL;
        public static RectTransform enableDisableButton;
        public static Sprite enabledSprite;
        public static Sprite disabledSprite;
        public static Sprite pausedSprite_planet;
        public static bool clearVegDropOnNextTick = false;

        [HarmonyPrefix, HarmonyPatch(typeof(GameMain), "Begin")]
        public static void GameMain_Begin_Prefix()
        {
            if (configEnableDebug.Value)
            {
                Logger.LogDebug("DSP Vege Drop: Calling - GameMain_Begin_Prefix.");
            }
            Config.Reload();
            if (GameMain.instance != null && GameObject.Find("Game Menu/button-1-bg") && enableDisableButton == null)
            {
                RectTransform parent = GameObject.Find("Game Menu").GetComponent<RectTransform>();
                RectTransform prefab = GameObject.Find("Game Menu/button-1-bg").GetComponent<RectTransform>();
                Vector3 referencePosition = GameObject.Find("Game Menu/button-1-bg").GetComponent<RectTransform>().localPosition;
                enableDisableButton = GameObject.Instantiate<RectTransform>(prefab);
                enableDisableButton.gameObject.name = "w-gates-vege-drop-enable-button";
                enableDisableButton.GetComponent<UIButton>().tips.tipTitle = configEnableMod.Value ? "Vege Drop Enabled" : "Vege Drop Disabled";
                enableDisableButton.GetComponent<UIButton>().tips.tipText = configEnableMod.Value ? "Click to disable Vege Drop" : "Click to enable Vege Drop";
                enableDisableButton.GetComponent<UIButton>().tips.delay = 0f;
                enableDisableButton.transform.Find("button-1/icon").GetComponent<Image>().sprite =
                    configEnableMod.Value ? enabledSprite : disabledSprite;
                enableDisableButton.SetParent(parent);
                enableDisableButton.localScale = new Vector3(0.35f, 0.35f, 0.35f);
                enableDisableButton.localPosition = new Vector3(referencePosition.x + 96f, referencePosition.y + 161f, referencePosition.z);
                enableDisableButton.GetComponent<UIButton>().button.onClick.AddListener(() =>
                {
                    configEnableMod.Value = !configEnableMod.Value;
                    OnConfigEnableChanged();
                });
            }
        }

        public static void UpdateTipText(String details)
        {
            enableDisableButton.transform.Find("button-1/icon").GetComponent<Image>().sprite =
                !configEnableMod.Value ? disabledSprite :
                enableSubstate == Substate.PLANET ? pausedSprite_planet :
                enabledSprite;
            enableDisableButton.GetComponent<UIButton>().tips.tipText = configEnableMod.Value ? "Click to disable vege drop" + "\n" + details : "Click to enable vege drop";
            enableDisableButton.GetComponent<UIButton>().UpdateTip();
        }

        public static Sprite GetSprite(Color color)
        {
            Texture2D tex = new Texture2D(48, 48, TextureFormat.RGBA32, false);

            // Draw a plane like the one representing drones in the Mecha Panel...
            // The in-game asset is called ui/textures/sprites/icons/drone-icon
            for (int x = 0; x < 48; x++)
            {
                for (int y = 0; y < 48; y++)
                {
                    if (((x >= 9) && (x <= 17) && (y >= 2) && (y <= 38)) ||
                        ((x >= 15) && (x <= 23) && (y >= 12) && (y <= 38)) ||
                        ((x >= 21) && (x <= 29) && (y >= 18) && (y <= 38)) ||
                        ((x >= 27) && (x <= 35) && (y >= 24) && (y <= 38)) ||
                        ((x >= 33) && (x <= 44) && (y >= 30) && (y <= 38)))
                    {
                        tex.SetPixel(x, y, color);
                    }
                    else
                    {
                        tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                    }
                }
            }

            tex.name = "w-gates-vege-drop-enable-icon";
            tex.Apply();

            return Sprite.Create(tex, new Rect(0f, 0f, 48f, 48f), new Vector2(0f, 0f), 1000);
        }

        public void InitialConfigSetup()
        {
            try
            {
                configEnableMod = Config.Bind<bool>("Config", "Enable", true, "Enable/disable vege drop mod.");
                configEnableDebug = Config.Bind<bool>("Config", "EnableDebug", false, "Enabling debug will add more feedback to the BepInEx console.  This includes the reasons why drones are not clearing.");

                configEnableClearingItemTree = Config.Bind<bool>("Items", "IncludeTrees", true, "Enabling drop of trees.");
                configEnableClearingItemStone = Config.Bind<bool>("Items", "IncludeStone", true, "Enabling drop of stones which can block the mecha's movement.");
                configEnableClearingItemDetail = Config.Bind<bool>("Items", "IncludePebbles", false, "Enabling drop of tiny stones which won't block the mecha's movement.");
                configEnableClearingItemIce = Config.Bind<bool>("Items", "IncludeIce", true, "Enabling drop of ice.");
                configDisableClearingItemIds_StringConfigEntry = Config.Bind<string>("Items", "DisableItemIds", "", "Disable drop of specific vege proto IDs.  String is a comma-separated list of shorts.  This mod will print to the debug console all vege proto IDs which are mined so you can see what IDs you're mining.  See README for this mod for more information.");

                configEnableClearingPlanetAridDesert = Config.Bind<bool>("Planets", "IncludeAridDesert", true, "Enable drop on arid desert planets.");
                configEnableClearingPlanetAshenGelisol = Config.Bind<bool>("Planets", "IncludeAshenGelisol", true, "Enable drop on ashen gelisol planets.");
                configEnableClearingPlanetBarrenDesert = Config.Bind<bool>("Planets", "IncludeBarrenDesert", true, "Enable drop on barren desert planets.");
                configEnableClearingPlanetGobi = Config.Bind<bool>("Planets", "IncludeGobi", true, "Enable drop on gobi planets.");
                configEnableClearingPlanetIceFieldGelisol = Config.Bind<bool>("Planets", "IncludeIceFieldGelisol", true, "Enable drop on ice field gelisol planets.");
                configEnableClearingPlanetLava = Config.Bind<bool>("Planets", "IncludeLava", true, "Enable drop on lava planets.");
                configEnableClearingPlanetMediterranean = Config.Bind<bool>("Planets", "IncludeMediterranean", true, "Enable drop on mediterranean planets.");
                configEnableClearingPlanetOceanWorld = Config.Bind<bool>("Planets", "IncludeOceanWorld", true, "Enable drop on ocean world planets.");
                configEnableClearingPlanetOceanicJungle = Config.Bind<bool>("Planets", "IncludeOceanicJungle", true, "Enable drop on oceanic jungle planets.");
                configEnableClearingPlanetPrairie = Config.Bind<bool>("Planets", "IncludePrairie", true, "Enable drop on prairie planets.");
                configEnableClearingPlanetRedStone = Config.Bind<bool>("Planets", "IncludeRedStone", true, "Enable drop on red stone (mushroom) planets.");
                configEnableClearingPlanetVolcanicAsh = Config.Bind<bool>("Planets", "IncludeVolcanicAsh", true, "Enable drop on volcanic ash planets.");

                configIconColor_enabled = Config.Bind<Color>("IconColors", "EnabledNormal", Color.green, new BepInEx.Configuration.ConfigDescription("The color of the drop icon when this mod is enabled."));
                configIconColor_disabled = Config.Bind<Color>("IconColors", "Disabled", Color.grey, new BepInEx.Configuration.ConfigDescription("The color of the drop icon when this mod is disabled."));
                configIconColor_planet = Config.Bind<Color>("IconColors", "PausedPlanet", Color.green, new BepInEx.Configuration.ConfigDescription("The color of the drop icon when drop is paused on the current planet type per configuration.  See configuration settings for planets."));
            }
            catch (Exception ex)
            {
                Logger.LogError($"InitialConfigSetup - Bind Config: {ex.Message}");
                throw ex;
            }

            try
            {
                OnConfigReload();
            }
            catch (Exception ex)
            {
                Logger.LogError($"InitialConfigSetup - OnConfigReload: {ex.Message}");
                throw ex;
            }

            try
            {
                Config.ConfigReloaded += OnConfigReload;
                Config.SettingChanged += OnConfigSettingChanged;
            }
            catch (Exception ex)
            {
                Logger.LogError($"InitialConfigSetup - Bind Config Callbacks: {ex.Message}");
                throw ex;
            }
        }

        public static void OnConfigReload(object sender, EventArgs e)
        {
            OnConfigReload();
        }

        public static void OnConfigReload()
        {
            try
            {
                OnConfigDisableItemIdsChanged();
            }
            catch (Exception ex)
            {
                Logger.LogError($"OnConfigReload - OnConfigDisableItemIdsChanged: {ex.Message}");
                throw ex;
            }

            try
            {
                OnConfigEnableChanged();
            }
            catch (Exception ex)
            {
                Logger.LogError($"OnConfigReload - OnConfigEnableChanged: {ex.Message}");
                throw ex;
            }

            try
            {
                OnConfigIconColorChanged();
            }
            catch (Exception ex)
            {
                Logger.LogError($"OnConfigReload - OnConfigIconColorChanged: {ex.Message}");
                throw ex;
            }

            try
            {
                OnConfigDisablePlanetsChanged();
            }
            catch (Exception ex)
            {
                Logger.LogError($"OnConfigReload - OnConfigDisablePlanetsChanged: {ex.Message}");
                throw ex;
            }

            Logger.LogInfo("Configuration loaded.");
        }

        public static void OnConfigSettingChanged(object sender, BepInEx.Configuration.SettingChangedEventArgs e)
        {
            BepInEx.Configuration.ConfigDefinition changedSetting = e.ChangedSetting.Definition;
            if (changedSetting.Section == "Config" && changedSetting.Key == "Enable")
            {
                OnConfigEnableChanged();
            }
            else if (changedSetting.Section == "Planets")
            {
                OnConfigDisablePlanetsChanged();
            }
            else if (changedSetting.Section == "Items" && changedSetting.Key == "DisableItemIds")
            {
                OnConfigDisableItemIdsChanged();
            }
            else if (changedSetting.Section == "IconColors")
            {
                OnConfigIconColorChanged();
            }
        }

        public static void OnConfigEnableChanged()
        {
            if (enableDisableButton != null)
            {
                if (!configEnableMod.Value)
                {
                    clearVegDropOnNextTick = true;
                }

                enableDisableButton.GetComponent<UIButton>().tips.tipTitle = configEnableMod.Value ? "Vege Drop Enabled" : "Vege Drop Disabled";
                UpdateTipText("");
            }
        }

        public static void OnConfigDisablePlanetsChanged()
        {
            try
            {
                if (configEnableDebug.Value)
                {
                    Logger.LogDebug($"OnConfigDisablePlanetsChanged: disabledPlanets - Before Config Change.");
                    Logger.LogDebug($"OnConfigDisablePlanetsChanged: disabledPlanets - {String.Join(",", disabledPlanets)}");
                }
                disabledPlanets.Clear();
                if (!configEnableClearingPlanetAridDesert.Value)
                    disabledPlanets.Add(configPlanetTypeMap[configEnableClearingPlanetAridDesert.Definition.Key]);
                if (!configEnableClearingPlanetAshenGelisol.Value)
                    disabledPlanets.Add(configPlanetTypeMap[configEnableClearingPlanetAshenGelisol.Definition.Key]);
                if (!configEnableClearingPlanetBarrenDesert.Value)
                    disabledPlanets.Add(configPlanetTypeMap[configEnableClearingPlanetBarrenDesert.Definition.Key]);
                if (!configEnableClearingPlanetGobi.Value)
                    disabledPlanets.Add(configPlanetTypeMap[configEnableClearingPlanetGobi.Definition.Key]);
                if (!configEnableClearingPlanetIceFieldGelisol.Value)
                    disabledPlanets.Add(configPlanetTypeMap[configEnableClearingPlanetIceFieldGelisol.Definition.Key]);
                if (!configEnableClearingPlanetLava.Value)
                    disabledPlanets.Add(configPlanetTypeMap[configEnableClearingPlanetLava.Definition.Key]);
                if (!configEnableClearingPlanetMediterranean.Value)
                    disabledPlanets.Add(configPlanetTypeMap[configEnableClearingPlanetMediterranean.Definition.Key]);
                if (!configEnableClearingPlanetOceanWorld.Value)
                    disabledPlanets.Add(configPlanetTypeMap[configEnableClearingPlanetOceanWorld.Definition.Key]);
                if (!configEnableClearingPlanetOceanicJungle.Value)
                    disabledPlanets.Add(configPlanetTypeMap[configEnableClearingPlanetOceanicJungle.Definition.Key]);
                if (!configEnableClearingPlanetPrairie.Value)
                    disabledPlanets.Add(configPlanetTypeMap[configEnableClearingPlanetPrairie.Definition.Key]);
                if (!configEnableClearingPlanetRedStone.Value)
                    disabledPlanets.Add(configPlanetTypeMap[configEnableClearingPlanetRedStone.Definition.Key]);
                if (!configEnableClearingPlanetVolcanicAsh.Value)
                    disabledPlanets.Add(configPlanetTypeMap[configEnableClearingPlanetVolcanicAsh.Definition.Key]);
                if (configEnableDebug.Value)
                {
                    Logger.LogDebug($"OnConfigDisablePlanetsChanged: disabledPlanets - After Config Change.");
                    Logger.LogDebug($"OnConfigDisablePlanetsChanged: disabledPlanets - {String.Join(",", disabledPlanets)}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"OnConfigDisablePlanetsChanged: {ex.Message}");
                throw ex;
            }
        }

        public static void OnConfigDisableItemIdsChanged()
        {
            if (configDisableClearingItemIds_StringConfigEntry != null)
            {
                configDisableClearingItemIds_ShortArray = configDisableClearingItemIds_StringConfigEntry.Value.Split(',').Select(s => short.TryParse(s, out short n) ? n : (short)0).ToArray();
                if (configDisableClearingItemIds_ShortArray.Length == 1 && configDisableClearingItemIds_ShortArray[0] == 0)
                {
                    configDisableClearingItemIds_ShortArray = new short[] { };
                }

                foreach (short protoId in configDisableClearingItemIds_ShortArray)
                {
                    VegeProto vegeProto = LDB.veges.Select((int)protoId);
                    if (vegeProto == null)
                    {
                        Logger.LogError($"ERROR: Configured vege proto ID {protoId} is invalid.  Recommend removing this ID from the config file.");
                    }
                    else
                    {
                        Logger.LogInfo($"Configured to block vege proto ID {protoId} for {vegeProto.Name.Translate()}");
                    }
                }
            }
        }

        public static void OnConfigIconColorChanged()
        {
            enabledSprite = GetSprite(configIconColor_enabled.Value);
            disabledSprite = GetSprite(configIconColor_disabled.Value);
            pausedSprite_planet = GetSprite(configIconColor_planet.Value);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(PlanetFactory), "RemoveVegeWithComponents",
            new Type[] { typeof(int) },
            new ArgumentType[] { ArgumentType.Normal }
        )]
        public static void PlanetFactory_RemoveVegeWithComponents_Prefix(
            ref PlanetFactory __instance, int id
        )
        {
            if (configEnableDebug.Value)
            {
                Logger.LogDebug("DSP Vege Drop: Calling - PlanetFactory_RemoveVegeWithComponents_Prefix.");
            }
            VegeData vegeData = __instance.vegePool[id];
            if (vegeData.id == 0)
            {
                return;
            }
            enableSubstate = Substate.NORMAL;

            if (!configEnableMod.Value)
            {
                if (configEnableDebug.Value)
                {
                    Logger.LogDebug("Skipping because mod is disabled.");
                }
                return;
            }

            // Filter based on planet.typeString
            // Themes are stored in Resources\prototypes\ThemeProtoSet.asset
            // Using DisplayName because it should be the same string regardless of translation.
            // Translations are stored in Resources\prototypes\StringProtoSet.asset
            string planetThemeName = LDB.themes.Select(__instance.planet.theme).DisplayName;
            if (disabledPlanets.Contains(planetThemeName))
            {
                if (configEnableDebug.Value)
                {
                    Logger.LogDebug($"Skipping planet type {__instance.planet.typeString}");
                }
                enableSubstate = Substate.PLANET;
                UpdateTipText("(Waiting on this planet type.)");
                return;
            }

            VegeProto vegeProto = LDB.veges.Select((int)vegeData.protoId);
            // vegeProto.Type == EVegeType.Detail covers grass and small minable rocks.  So the check includes vegeProto.MiningItem.Length instead.
            // VegeProto stored in Resources\prototypes\VegeProtoSet.asset
            if (vegeProto != null && vegeProto.MiningItem.Length > 0)
            {
                if (vegeProto.Name == "飞行舱") // Space Capsule
                {
                    if (configEnableDebug.Value)
                    {
                        Logger.LogDebug($"Skipping Space Capsule");
                    }
                    return;
                }
                else if ((vegeProto.Type == EVegeType.Tree && !configEnableClearingItemTree.Value) ||
                    (vegeProto.Type == EVegeType.Stone && !configEnableClearingItemStone.Value) ||
                    (vegeProto.Type == EVegeType.Detail && !configEnableClearingItemDetail.Value) ||
                    (vegeProto.Type == EVegeType.Ice && !configEnableClearingItemIce.Value))
                {
                    if (configEnableDebug.Value)
                    {
                        Logger.LogDebug($"Skipping Disabled Veg Type {vegeProto.Type.ToString()}");
                    }
                    return;
                }
            }

            bool disabledById = false;
            foreach (short disabledId in configDisableClearingItemIds_ShortArray)
            {
                if (vegeData.protoId == disabledId)
                {
                    disabledById = true;
                    break;
                }
            }
            if (disabledById)
            {
                if (configEnableDebug.Value)
                {
                    Logger.LogDebug($"Skipping Disabled Veg ID {vegeData.protoId.ToString()}");
                }
                return;
            }
            System.Random random = new System.Random(vegeData.id + ((__instance.planet.seed & 16383) << 14));
            int popupQueueIndex = 0;
            for (int itemIdx = 0; itemIdx < vegeProto.MiningItem.Length; itemIdx++)
            {
                float randomMiningChance = (float)random.NextDouble();
                if (randomMiningChance < vegeProto.MiningChance[itemIdx])
                {
                    int minedItem = vegeProto.MiningItem[itemIdx];
                    int minedItemCount = (int)((float)vegeProto.MiningCount[itemIdx] * (vegeData.scl.y * vegeData.scl.y) + 0.5f);
                    if (minedItemCount > 0 && LDB.items.Select(minedItem) != null)
                    {
                        int inventoryOverflowCount = GameMain.mainPlayer.TryAddItemToPackage(minedItem, minedItemCount, 0, true, 0);
                        GameMain.statistics.production.factoryStatPool[__instance.index].AddProductionToTotalArray(minedItem, minedItemCount);
                        GameMain.mainPlayer.controller.gameData.history.AddFeatureValue(2150000 + minedItem, minedItemCount);

                        UIItemup.Up(minedItem, inventoryOverflowCount);
                        UIRealtimeTip.PopupItemGet(minedItem, inventoryOverflowCount, vegeData.pos + vegeData.pos.normalized, popupQueueIndex++);
                    }
                }
            }

            if (configEnableDebug.Value)
            {
                Logger.LogDebug($"Dropped proto ID {vegeProto.ID} (" + vegeProto.Name + ": " + vegeProto.Name.Translate() + ")");
            }
        }


        public static long lastDisplayTime = 0;

        [HarmonyPostfix, HarmonyPatch(typeof(GameData), "Destroy")]
        public static void GameData_Destroy_Postfix()
        {
            if (configEnableDebug.Value)
            {
                Logger.LogDebug("DSP Drone Clearing - Calling: GameData_Destroy_Postfix");
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GameScenarioLogic), "NotifyOnVegetableMined")]
        public static void GameScenarioLogic_NotifyOnVegetableMined_Prefix(int protoId)
        {
            if (configEnableDebug.Value)
            {
                Logger.LogDebug("DSP Drone Clearing - Calling: GameScenarioLogic_NotifyOnVegetableMined_Prefix");
                VegeProto vegeProto = LDB.veges.Select(protoId);
                Logger.LogDebug($"Mined proto ID {protoId} (" + vegeProto.Name + ": " + vegeProto.Name.Translate() + ")");
            }
        }
    }
}
