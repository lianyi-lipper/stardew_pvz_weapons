/*
 * Stardew PvZ Weapons Mod
 * 模块: Integration
 * 用途: Harmony 补丁 - 在物品创建时注入 modData
 */

namespace StardewPvZWeapons.Integration
{
    using HarmonyLib;
    using StardewValley;
    using StardewValley.Tools;
    using System;

    /// <summary>
    /// 物品创建补丁
    /// 在弹弓创建后根据 ItemId 自动注入 modData
    /// </summary>
    public static class CraftingRecipePatch
    {
        private const string WEAPON_ID = "lianyi-lipper.StardewPvZWeapons_ElectricGatlingPea";

        /// <summary>
        /// 应用 Harmony 补丁
        /// </summary>
        public static void Apply(Harmony harmony)
        {
            // 拦截 Slingshot 构造函数，在创建时注入 modData
            harmony.Patch(
                original: AccessTools.Constructor(typeof(Slingshot), new Type[] { typeof(string) }),
                postfix: new HarmonyMethod(typeof(CraftingRecipePatch), nameof(Slingshot_Constructor_Postfix))
            );
        }

        /// <summary>
        /// 后缀补丁 - Slingshot 构造函数完成后检查并注入 modData
        /// </summary>
        private static void Slingshot_Constructor_Postfix(Slingshot __instance, string itemId)
        {
            // 如果是我们的自定义武器，注入 modData
            if (itemId == WEAPON_ID || itemId == "(W)" + WEAPON_ID)
            {
                __instance.modData["lianyi-lipper.StardewPvZWeapons/WeaponType"] = "ElectricGatlingPea";
                __instance.modData["lianyi-lipper.StardewPvZWeapons/WeaponName"] = "电能超级机枪射手";
            }
        }
    }
}
