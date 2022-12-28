using System;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using PotionCraft.QuestSystem;
using PotionCraft.ManagersSystem;
using PotionCraft.ScriptableObjects;
using PotionCraft.LocalizationSystem;
using PotionCraft.ScriptableObjects.Salts;
using PotionCraft.Npc.MonoBehaviourScripts;
using PotionCraft.ScriptableObjects.Potion;
using PotionCraft.ScriptableObjects.Ingredient;
using PotionCraft.ObjectBased.UIElements.Tooltip;
using PotionCraft.ObjectBased.UIElements.Dialogue;
using PotionCraft.ScriptableObjects.AlchemyMachineProducts;
using PotionCraft.ObjectBased.RecipeMap.RecipeMapItem.SolventDirectionHint;

namespace xiaoye97
{
    [BepInPlugin("me.xiaoye97.plugin.PotionCraft.MoreInformation", "MoreInformation", "1.0.0")]
    public class MoreInformation : BaseUnityPlugin
    {
        public static string goldIcon = "<sprite=\"CommonAtlas\" name=\"Gold Icon\">";
        public static Sprite solventDirectionLine;

        private void Start()
        {
            LocalizationManager.OnInitialize.AddListener(SetModLocalization);
            solventDirectionLine = LoadSprite("Solvent Direction Line.png");
            Harmony.CreateAndPatchAll(typeof(MoreInformation));
        }

        #region Mod多语言

        public static void RegisterLoc(string key, string en, string zh)
        {
            for (int i = 0; i <= (int)LocalizationManager.Locale.cs; i++)
            {
                if ((LocalizationManager.Locale)i == LocalizationManager.Locale.zh)
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
            RegisterLoc("#mod_moreinformation_nothas", "<color=red>Not has, purchase recommended</color>", "<color=red>未拥有，建议购入</color>");
        }

        #endregion Mod多语言

        #region 药剂方向提示线

        public static Texture2D LoadResTexture2D(string name)
        {
            var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("PotionCraft_MoreInformation." + name);
            int length = (int)s.Length;
            byte[] bs = new byte[length];
            s.Read(bs, 0, length);
            s.Close();
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(bs);
            return tex;
        }

        public static Sprite LoadSprite(string name)
        {
            var tex = LoadResTexture2D(name);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0));
            return sprite;
        }

        /// <summary>
        /// 药剂方向提示线
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(SolventDirectionHint), "Awake")]
        public static void SolventDirectionHint_Awake_Patch(SolventDirectionHint __instance)
        {
            __instance.spriteRenderer.sprite = solventDirectionLine;
        }

        #endregion 药剂方向提示线

        #region Tooltip

        public static string GetPriceString(InventoryItem item, int count = 1)
        {
            return goldIcon + " " + item.GetPrice() * count;
        }

        public static void AddNormalPriceTooltip(TooltipContent tooltip, InventoryItem item, bool notHasTip = false)
        {
            tooltip.description2 += $"{LocalizationManager.GetText("#mod_moreinformation_value")}\t {GetPriceString(item)}";
            int hasCount = Managers.Player.inventory.GetItemCount(item);
            if (hasCount > 0)
            {
                tooltip.description2 += $"\n{LocalizationManager.GetText("#mod_moreinformation_has")} {hasCount}\t " + GetPriceString(item, hasCount);
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
                    cost += ((InventoryItem)c.componentObject).GetPrice() * c.amount;
                }
            }
            __result.description2 += $"\n{LocalizationManager.GetText("#mod_moreinformation_cost")}\t {goldIcon} {cost}";
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
        public static void LegendarySaltPile_GetTooltipContent_Patch(LegendarySaltPile __instance, ref TooltipContent __result)
        {
            AddNormalPriceTooltip(__result, __instance);
        }

        /// <summary>
        /// 传说物质信息
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(LegendarySubstance), "GetTooltipContent")]
        public static void LegendarySubstance_GetTooltipContent_Patch(LegendarySubstance __instance, ref TooltipContent __result)
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
            NpcMonoBehaviour currentNpcMonoBehaviour = Managers.Npc.CurrentNpcMonoBehaviour;
            Quest currentQuest = currentNpcMonoBehaviour.currentQuest;
            string str = "";
            foreach (var effect in currentQuest.desiredEffects)
            {
                str += new Key("#effect_" + effect.name, null, false).GetText() + " ";
            }
            __instance.dialogueText.text.text += $"\n<color=#a39278>{str}</color>";
        }

        #endregion 目标药剂效果提醒
    }
}