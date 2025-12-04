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
            // 注册控制台指令
            _helper.ConsoleCommands.Add(
                name: "pvz_give_weapon",
                documentation: "给予PvZ武器。用法: pvz_give_weapon <weapon_name>\n可用武器: primal_mangosteen",
                callback: OnConsoleCommand
            );

            _monitor.Log("武器注册系统已初始化", LogLevel.Debug);
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
                    Game1.addHUDMessage(new HUDMessage($"获得：{trinket.DisplayName}", 2));
                    Game1.addHUDMessage(new HUDMessage("装备到饰品栏即可使用", 1));
                }
                else
                {
                    _monitor.Log("背包已满，无法添加饰品", LogLevel.Warn);
                    Game1.addHUDMessage(new HUDMessage("背包已满！", 3));
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
                    Game1.addHUDMessage(new HUDMessage("获得：电能超级机枪射手", 2));
                    Game1.addHUDMessage(new HUDMessage("左键攻击，右键大招", 1));
                }
                else
                {
                    _monitor.Log("背包已满，无法添加武器", LogLevel.Warn);
                    Game1.addHUDMessage(new HUDMessage("背包已满！", 3));
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
