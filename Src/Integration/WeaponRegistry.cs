/*
 * Stardew PvZ Weapons Mod
 * 模块: Integration
 * 用途: 武器注册系统，将自定义武器添加到游戏中
 */

namespace StardewPvZWeapons.Integration
{
    using System;
    using StardewModdingAPI;
    using StardewValley;
    using StardewPvZWeapons.Domain.Weapons;

    /// <summary>
    /// 武器注册器
    /// 负责将自定义武器注册到游戏系统
    /// </summary>
    public class WeaponRegistry
    {
        private readonly IModHelper _helper;
        private readonly IMonitor _monitor;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="helper">SMAPI Helper</param>
        /// <param name="monitor">日志监视器</param>
        public WeaponRegistry(IModHelper helper, IMonitor monitor)
        {
            _helper = helper;
            _monitor = monitor;
        }

        /// <summary>
        /// 初始化武器注册系统
        /// 注册控制台指令
        /// </summary>
        public void Initialize()
        {
            // 注册控制台指令 - 直接给予
            _helper.ConsoleCommands.Add(
                name: "pvz_give_weapon",
                documentation: "给予PvZ武器（调试用）。用法: pvz_give_weapon <weapon_name>\n可用武器: primal_mangosteen, electric_gatling_pea",
                callback: OnConsoleCommand
            );

            // 注册控制台指令 - 合成（消耗材料）
            _helper.ConsoleCommands.Add(
                name: "pvz_craft",
                documentation: "使用材料合成PvZ武器。用法: pvz_craft <weapon_name>\n可用配方: electric_gatling_pea (电池组x5 + 铱锭x10)",
                callback: OnCraftCommand
            );

            _monitor.Log("Weapon registry initialized", LogLevel.Trace);
        }

        /// <summary>
        /// 控制台指令回调
        /// </summary>
        /// <param name="command">指令名称</param>
        /// <param name="args">参数列表</param>
        private void OnConsoleCommand(string command, string[] args)
        {
            if (!Context.IsWorldReady)
            {
                _monitor.Log("错误：必须在游戏加载后使用此命令", LogLevel.Error);
                return;
            }

            if (args.Length == 0)
            {
                _monitor.Log("用法: pvz_give_weapon <weapon_name>", LogLevel.Info);
                _monitor.Log("可用武器:", LogLevel.Info);
                _monitor.Log("  - primal_mangosteen (聚能山竹饰品)", LogLevel.Info);
                _monitor.Log("  - electric_gatling_pea (电能超级机枪射手弹弓)", LogLevel.Info);
                return;
            }

            string weaponName = args[0].ToLower();

            switch (weaponName)
            {
                case "primal_mangosteen":
                case "mangosteen":
                case "山竹":
                    GivePrimalMangosteen();
                    break;

                case "electric_gatling_pea":
                case "gatling":
                case "机枪":
                    GiveElectricGatlingPea();
                    break;

                default:
                    _monitor.Log($"未知的武器: {weaponName}", LogLevel.Error);
                    _monitor.Log("可用武器: primal_mangosteen, electric_gatling_pea", LogLevel.Info);
                    break;
            }
        }

        /// <summary>
        /// 合成命令回调
        /// </summary>
        private void OnCraftCommand(string command, string[] args)
        {
            if (!Context.IsWorldReady)
            {
                _monitor.Log("错误：必须在游戏加载后使用此命令", LogLevel.Error);
                return;
            }

            if (args.Length == 0)
            {
                _monitor.Log("用法: pvz_craft <weapon_name>", LogLevel.Info);
                _monitor.Log("可用配方:", LogLevel.Info);
                _monitor.Log("  - electric_gatling_pea (电池组x5 + 铱锭x10)", LogLevel.Info);
                return;
            }

            string weaponName = args[0].ToLower();

            switch (weaponName)
            {
                case "electric_gatling_pea":
                case "gatling":
                case "机枪":
                    CraftElectricGatlingPea();
                    break;

                default:
                    _monitor.Log($"未知的配方: {weaponName}", LogLevel.Error);
                    _monitor.Log("可用配方: electric_gatling_pea", LogLevel.Info);
                    break;
            }
        }

        /// <summary>
        /// 合成电能超级机枪射手（消耗材料）
        /// 配方: 电池组(787) x5 + 铱锭(337) x10
        /// </summary>
        private void CraftElectricGatlingPea()
        {
            var player = Game1.player;
            if (player == null)
            {
                _monitor.Log("错误：无法找到玩家", LogLevel.Error);
                return;
            }

            // 检查材料
            const string BATTERY_PACK_ID = "787";  // 电池组
            const string IRIDIUM_BAR_ID = "337";   // 铱锭
            const int BATTERY_REQUIRED = 5;
            const int IRIDIUM_REQUIRED = 10;

            int batteryCount = 0;
            int iridiumCount = 0;

            // 统计背包中的材料数量
            foreach (var item in player.Items)
            {
                if (item != null)
                {
                    if (item.ItemId == BATTERY_PACK_ID)
                        batteryCount += item.Stack;
                    else if (item.ItemId == IRIDIUM_BAR_ID)
                        iridiumCount += item.Stack;
                }
            }

            // 检查材料是否足够
            if (batteryCount < BATTERY_REQUIRED || iridiumCount < IRIDIUM_REQUIRED)
            {
                _monitor.Log($"材料不足！需要: 电池组x{BATTERY_REQUIRED}, 铱锭x{IRIDIUM_REQUIRED}", LogLevel.Warn);
                _monitor.Log($"当前拥有: 电池组x{batteryCount}, 铱锭x{iridiumCount}", LogLevel.Info);
                Game1.addHUDMessage(new HUDMessage(string.Format(_helper.Translation.Get("hud.materials-insufficient"), BATTERY_REQUIRED, IRIDIUM_REQUIRED), 3));
                return;
            }

            // 消耗材料
            int batteryToRemove = BATTERY_REQUIRED;
            int iridiumToRemove = IRIDIUM_REQUIRED;

            for (int i = 0; i < player.Items.Count && (batteryToRemove > 0 || iridiumToRemove > 0); i++)
            {
                var item = player.Items[i];
                if (item == null) continue;

                if (item.ItemId == BATTERY_PACK_ID && batteryToRemove > 0)
                {
                    int remove = Math.Min(item.Stack, batteryToRemove);
                    item.Stack -= remove;
                    batteryToRemove -= remove;
                    if (item.Stack <= 0)
                        player.Items[i] = null;
                }
                else if (item.ItemId == IRIDIUM_BAR_ID && iridiumToRemove > 0)
                {
                    int remove = Math.Min(item.Stack, iridiumToRemove);
                    item.Stack -= remove;
                    iridiumToRemove -= remove;
                    if (item.Stack <= 0)
                        player.Items[i] = null;
                }
            }

            // 给予武器
            GiveElectricGatlingPea();
            _monitor.Log("Crafting success! Consumed Battery x5 and Iridium Bar x10", LogLevel.Trace);
        }

        /// <summary>
        /// 给予玩家聚能山竹饰品
        /// </summary>
        private void GivePrimalMangosteen()
        {
            try
            {
                var player = Game1.player;
                if (player == null)
                {
                    _monitor.Log("错误：无法找到玩家", LogLevel.Error);
                    return;
                }

                // 通过ItemRegistry创建饰品实例
                var trinket = StardewValley.ItemRegistry.Create(
                    "(TR)lianyi-lipper.StardewPvZWeapons_PrimalMangosteen",
                    1, // 数量
                    0  // quality
                );

                if (trinket == null)
                {
                    _monitor.Log("错误：无法创建饰品（可能数据未正确注册）", LogLevel.Error);
                    return;
                }

                // 尝试添加到背包
                if (player.addItemToInventoryBool(trinket))
                {
                    _monitor.Log($"已将「{trinket.DisplayName}」添加到背包", LogLevel.Info);
                    _monitor.Log("提示：打开背包，将饰品拖到饰品栏装备", LogLevel.Info);
                    
                    // 在游戏中显示提示
                    Game1.addHUDMessage(new HUDMessage(string.Format(_helper.Translation.Get("hud.trinket.obtained"), trinket.DisplayName), 2));
                    Game1.addHUDMessage(new HUDMessage(_helper.Translation.Get("hud.trinket.equip-hint"), 1));
                }
                else
                {
                    _monitor.Log("背包已满，无法添加饰品", LogLevel.Warn);
                    Game1.addHUDMessage(new HUDMessage(_helper.Translation.Get("hud.inventory-full"), 3));
                }
            }
            catch (Exception ex)
            {
                _monitor.Log($"添加饰品时出错: {ex.Message}", LogLevel.Error);
                _monitor.Log(ex.StackTrace ?? "", LogLevel.Trace);
            }
        }

        /// <summary>
        /// 给予玩家电能超级机枪射手弹弓
        /// </summary>
        private void GiveElectricGatlingPea()
        {
            try
            {
                var player = Game1.player;
                if (player == null)
                {
                    _monitor.Log("错误：无法找到玩家", LogLevel.Error);
                    return;
                }

                // ✅ 创建原生弹弓对象（避免序列化问题）
                var slingshot = new StardewValley.Tools.Slingshot("lianyi-lipper.StardewPvZWeapons_ElectricGatlingPea");
                
                // ✅ 使用 modData 标记这是自定义武器
                slingshot.modData["lianyi-lipper.StardewPvZWeapons/WeaponType"] = "ElectricGatlingPea";
                slingshot.modData["lianyi-lipper.StardewPvZWeapons/WeaponName"] = "电能超级机枪射手";
                
                // 尝试添加到背包
                if (player.addItemToInventoryBool(slingshot))
                {
                    _monitor.Log($"已将「电能超级机枪射手」添加到背包", LogLevel.Info);
                    _monitor.Log("提示：选中武器，左键攻击，右键触发大招", LogLevel.Info);
                    
                    // 在游戏中显示提示
                    Game1.addHUDMessage(new HUDMessage(_helper.Translation.Get("weapon.electric-gatling-pea.craft.success"), 2));
                    Game1.addHUDMessage(new HUDMessage(_helper.Translation.Get("weapon.electric-gatling-pea.craft.hint"), 1));
                }
                else
                {
                    _monitor.Log("背包已满，无法添加武器", LogLevel.Warn);
                    Game1.addHUDMessage(new HUDMessage(_helper.Translation.Get("hud.inventory-full"), 3));
                }
            }
            catch (Exception ex)
            {
                _monitor.Log($"添加武器时出错: {ex.Message}", LogLevel.Error);
                _monitor.Log(ex.StackTrace ?? "", LogLevel.Trace);
            }
        }

        /// <summary>
        /// 给予玩家指定武器（通用方法，供未来扩展使用）
        /// </summary>
        /// <param name="weapon">武器实例</param>
        /// <returns>是否成功添加</returns>
        public bool GiveWeapon(ICustomWeapon weapon)
        {
            try
            {
                var player = Game1.player;
                if (player == null) return false;

                if (weapon is StardewValley.Tool tool)
                {
                    return player.addItemToInventoryBool(tool);
                }

                return false;
            }
            catch (Exception ex)
            {
                _monitor.Log($"添加武器失败: {ex.Message}", LogLevel.Error);
                return false;
            }
        }
    }
}
