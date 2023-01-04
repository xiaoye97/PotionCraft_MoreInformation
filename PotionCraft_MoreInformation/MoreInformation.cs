using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using PotionCraft.QuestSystem;
using PotionCraft.ManagersSystem;
using PotionCraft.ScriptableObjects;
using PotionCraft.LocalizationSystem;
using PotionCraft.ObjectBased.Mortar;
using PotionCraft.ScriptableObjects.Salts;
using PotionCraft.Npc.MonoBehaviourScripts;
using PotionCraft.ScriptableObjects.Potion;
using PotionCraft.DebugObjects.DebugWindows;
using PotionCraft.ObjectBased.InteractiveItem;
using PotionCraft.ScriptableObjects.Ingredient;
using PotionCraft.ObjectBased.UIElements.Tooltip;
using PotionCraft.ObjectBased.UIElements.Dialogue;
using PotionCraft.ScriptableObjects.AlchemyMachineProducts;
using PotionCraft.ObjectBased.RecipeMap.RecipeMapItem.IndicatorMapItem;
using PotionCraft.ObjectBased.RecipeMap.RecipeMapItem.SolventDirectionHint;

namespace xiaoye97
{
    [BepInPlugin("me.xiaoye97.plugin.PotionCraft.MoreInformation", "MoreInformation", "1.2.0")]
    public class MoreInformation : BaseUnityPlugin
    {
        public static string goldIcon = "<sprite=\"CommonAtlas\" name=\"Gold Icon\">";

        private void Start()
        {
            BindConfigs();
            LocalizationManager.OnInitialize.AddListener(SetModLocalization);
            solventDirectionLine = Helper.LoadSprite("Solvent Direction Line.png");
            Harmony.CreateAndPatchAll(typeof(MoreInformation));
        }
        
        #region 配置注册

        private static ConfigEntry<bool> _configClearLine;
        private static ConfigEntry<bool> _configToolTip;
        private static ConfigEntry<bool> _configDemandPotion;
        private static ConfigEntry<bool> _configTransparentBottle;
        private static ConfigEntry<bool> _configGrindProgress;

        private void BindConfigs()
        {
            _configClearLine = Config.Bind(
                "General", "ClearLine", true,
                "Render a distinct line between potion and origin. \n药水和原点的连线现在变得更加清晰。");
            _configToolTip = Config.Bind(
                "General", "ToolTips", true,
                "Tooltip now shows the item's value, the number you own and the total value, and the cost of the potion. \n物品的提示信息中，现在会显示物品的价值，自己拥有的数量以及总价值，药水的成本。");
            _configDemandPotion = Config.Bind(
                "General", "DemandPotion", true,
                "The customer NPC's dialogue will show the potion he wants. \n顾客NPC的对话中，会显示他想要的属性。");
            _configTransparentBottle = Config.Bind(
                "General", "DemandPotion", true,
                "When you mouse over the potion bottle, the bottle will become translucent and you can see the covered lines. \n当鼠标覆盖到药瓶时，药瓶会变成半透明，可以看到被盖住的线。");
            _configGrindProgress = Config.Bind(
                "General", "GrindProgress", true,
                "Display the grinding progress of ingredients in percentage. \n实时显示药材的研磨进度。");
        }
        #endregion

        #region Mod多语言

        public static void RegisterLoc(string key, string en, string zh)
        {
            for (int i = 0; i <= (int) LocalizationManager.Locale.cs; i++)
            {
                if ((LocalizationManager.Locale) i == LocalizationManager.Locale.zh)
                {
                    LocalizationManager.localizationData.Add(i, key, zh);
                }
                else
                {
                    LocalizationManager.localizationData.Add(i, key, en);
                }
            }
        }

        public static void SetModLocalization()
        {
            RegisterLoc("#mod_moreinformation_value", "Value", "价值");
            RegisterLoc("#mod_moreinformation_cost", "Cost", "成本");
            RegisterLoc("#mod_moreinformation_has", "Has", "已拥有");
            RegisterLoc("#mod_moreinformation_nothas", "<color=red>Items not owned, recommended</color>",
                "<color=red>未拥有，建议购入</color>");
        }

        #endregion Mod多语言

        #region 药剂方向提示线

        public static Sprite solventDirectionLine;

        /// <summary>
        /// 药剂方向提示线
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(SolventDirectionHint), "Awake")]
        public static void SolventDirectionHint_Awake_Patch(SolventDirectionHint __instance)
        {
            if (!_configClearLine.Value) return;
            __instance.spriteRenderer.sprite = solventDirectionLine;
        }

        #endregion 药剂方向提示线

        #region Tooltip

        public static string GetPriceString(InventoryItem item, int count = 1)
        {
            return goldIcon + " " + (item.GetPrice() * count).ToString("0.##");
        }

        public static void AddNormalPriceTooltip(TooltipContent tooltip, InventoryItem item, bool notHasTip = false)
        {
            if (!_configToolTip.Value) return;
            tooltip.description2 +=
                $"{LocalizationManager.GetText("#mod_moreinformation_value")}\t {GetPriceString(item)}";
            int hasCount = Managers.Player.inventory.GetItemCount(item);
            if (hasCount > 0)
            {
                tooltip.description2 += $"\n{LocalizationManager.GetText("#mod_moreinformation_has")} {hasCount}\t " +
                                        GetPriceString(item, hasCount);
            }
            else
            {
                if (notHasTip)
                {
                    tooltip.description2 += $"\n{LocalizationManager.GetText("#mod_moreinformation_nothas")}";
                }
            }
        }

        /// <summary>
        /// 原材料信息
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(Ingredient), "GetTooltipContent")]
        public static void Ingredient_GetTooltipContent_Patch(Ingredient __instance, ref TooltipContent __result)
        {
            AddNormalPriceTooltip(__result, __instance, true);
        }

        /// <summary>
        /// 药水信息
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(Potion), "GetTooltipContent")]
        public static void Potion_GetTooltipContent_Patch(Potion __instance, ref TooltipContent __result)
        {
            AddNormalPriceTooltip(__result, __instance);
            float cost = 0;
            foreach (var c in __instance.usedComponents)
            {
                if (c.componentType == PotionUsedComponent.ComponentType.InventoryItem)
                {
                    cost += ((InventoryItem) c.componentObject).GetPrice() * c.amount;
                }
            }

            __result.description2 +=
                $"\n{LocalizationManager.GetText("#mod_moreinformation_cost")}\t {goldIcon} {cost.ToString("0.##")}";
        }

        /// <summary>
        /// 盐信息
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(Salt), "GetTooltipContent")]
        public static void Salt_GetTooltipContent_Patch(Salt __instance, ref TooltipContent __result)
        {
            AddNormalPriceTooltip(__result, __instance);
        }

        /// <summary>
        /// 传说盐信息
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(LegendarySaltPile), "GetTooltipContent")]
        public static void LegendarySaltPile_GetTooltipContent_Patch(LegendarySaltPile __instance,
            ref TooltipContent __result)
        {
            AddNormalPriceTooltip(__result, __instance);
        }

        /// <summary>
        /// 传说物质信息
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(LegendarySubstance), "GetTooltipContent")]
        public static void LegendarySubstance_GetTooltipContent_Patch(LegendarySubstance __instance,
            ref TooltipContent __result)
        {
            AddNormalPriceTooltip(__result, __instance);
        }

        #endregion Tooltip

        #region 目标药剂效果提醒

        /// <summary>
        /// 目标药剂效果提醒
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(DialogueBox), "UpdatePotionRequestText")]
        public static void DialogueBox_UpdatePotionRequestText_Patch(DialogueBox __instance)
        {
            if (!_configDemandPotion.Value) return;
            NpcMonoBehaviour currentNpcMonoBehaviour = Managers.Npc.CurrentNpcMonoBehaviour;
            Quest currentQuest = currentNpcMonoBehaviour.currentQuest;
            string str = "";
            foreach (var effect in currentQuest.desiredEffects)
            {
                str += new Key("#effect_" + effect.name, null, false).GetText() + " ";
            }

            __instance.dialogueText.text.text += $"<color=#a39278>{str}</color>";
        }

        #endregion 目标药剂效果提醒

        #region 药瓶瓶身半透明

        [HarmonyPostfix, HarmonyPatch(typeof(InteractiveItem), "Hover")]
        public static void InteractiveItem_UpdateHover_Patch(InteractiveItem __instance, bool hover)
        {
            if (!_configTransparentBottle.Value) return;

            if (__instance is IndicatorMapItem)
            {
                var item = __instance as IndicatorMapItem;
                if (hover)
                {
                    item.backgroundSpriteRenderer.enabled = false;
                    item.liquidColorChangeAnimator.upperContainer.SetAlpha(0.1f);
                    item.liquidColorChangeAnimator.lowerContainer.SetAlpha(0.1f);
                }
                else
                {
                    item.backgroundSpriteRenderer.enabled = true;
                    item.liquidColorChangeAnimator.upperContainer.SetAlpha(1);
                    item.liquidColorChangeAnimator.lowerContainer.SetAlpha(1);
                }
            }
        }

        #endregion 药瓶瓶身半透明

        #region 研磨进度

        public static DebugWindow GrindStatusDebugWindow;

        private static void InitGrindStatusDebugWindow()
        {
            if (GrindStatusDebugWindow == null)
            {
                GrindStatusDebugWindow = Helper.CreateClearDebugWindow("研磨进度", new Vector2(4, -5));
                var room = Managers.Room.instantiatedRooms[
                    (int) PotionCraft.ManagersSystem.Room.RoomManager.RoomIndex.Laboratory];
                GrindStatusDebugWindow.transform.SetParent(room.transform, false);
            }

            GrindStatusDebugWindow.transform.localPosition = new Vector3(4, -5, 0);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Mortar), "Update")]
        public static void Mortar_Update_Patch(Mortar __instance)
        {
            if (!_configGrindProgress.Value) return;

            InitGrindStatusDebugWindow();
            if (__instance.containedStack != null)
            {
                float status = Mathf.Clamp01(__instance.containedStack.overallGrindStatus);
                GrindStatusDebugWindow.ShowText($"{(status * 100).ToString("f2")}%");
            }
            else
            {
                GrindStatusDebugWindow.ShowText("");
            }
        }

        #endregion 研磨进度
    }
}