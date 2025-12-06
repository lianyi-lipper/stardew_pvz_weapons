/*
 * Stardew PvZ Weapons Mod
 * 模块: Data
 * 用途: 武器配置加载器
 */

namespace StardewPvZWeapons.Data
{
    using StardewModdingAPI;
    using System;

    /// <summary>
    /// 武器配置加载器
    /// 负责从 JSON 文件加载配置
    /// </summary>
    public class WeaponConfigLoader
    {
        private readonly IModHelper _helper;
        private readonly IMonitor _monitor;
        private WeaponsConfigFile? _config;

        public WeaponConfigLoader(IModHelper helper, IMonitor monitor)
        {
            _helper = helper;
            _monitor = monitor;
        }

        /// <summary>
        /// 加载配置文件
        /// </summary>
        public WeaponsConfigFile Load()
        {
            try
            {
                _config = _helper.Data.ReadJsonFile<WeaponsConfigFile>("Assets/Data/weapons_config.json");
                
                if (_config == null)
                {
                    _monitor.Log("配置文件未找到或为空，使用默认配置", LogLevel.Warn);
                    _config = CreateDefaultConfig();
                }
                else
                {
                    _monitor.Log("Weapon config loaded successfully", LogLevel.Trace);
                    LogConfigValues();
                }

                return _config;
            }
            catch (Exception ex)
            {
                _monitor.Log($"加载配置文件失败: {ex.Message}", LogLevel.Error);
                return CreateDefaultConfig();
            }
        }

        /// <summary>
        /// 创建默认配置（回退方案）
        /// </summary>
        private WeaponsConfigFile CreateDefaultConfig()
        {
            return new WeaponsConfigFile
            {
                PrimalMangosteen = new PrimalMangosteenConfig
                {
                    BaseDamage = 70,
                    AttackRange = 3,
                    CooldownTime = 2.0f,
                    ParalyzeChance = 1.0f,
                    ParalyzeDuration = 1.0f,
                    ElectricRing = new ElectricRingConfig
                    {
                        ExpandDuration = 0.25f,
                        SustainDuration = 0.1f,
                        FadeDuration = 0.15f,
                        DamageCheckInterval = 0.05f
                    },
                    UltimateAbility = new UltimateAbilityConfig
                    {
                        Range = 5,
                        Damage = 200,
                        Cooldown = 60.0f
                    }
                }
            };
        }

        /// <summary>
        /// 记录配置值（调试用）
        /// </summary>
        private void LogConfigValues()
        {
            if (_config?.PrimalMangosteen == null) return;

            var pm = _config.PrimalMangosteen;
            _monitor.Log($"  PrimalMangosteen config:", LogLevel.Trace);
            _monitor.Log($"    BaseDamage: {pm.BaseDamage}", LogLevel.Trace);
            _monitor.Log($"    AttackRange: {pm.AttackRange}", LogLevel.Trace);
            _monitor.Log($"    CooldownTime: {pm.CooldownTime}s", LogLevel.Trace);
            _monitor.Log($"    UltimateDamage: {pm.UltimateAbility?.Damage}", LogLevel.Trace);
            _monitor.Log($"    UltimateCooldown: {pm.UltimateAbility?.Cooldown}s", LogLevel.Trace);
        }

        /// <summary>
        /// 获取聚能山竹配置
        /// </summary>
        public PrimalMangosteenConfig GetPrimalMangosteenConfig()
        {
            if (_config?.PrimalMangosteen == null)
            {
                _monitor.Log("配置未加载，返回默认配置", LogLevel.Warn);
                return new PrimalMangosteenConfig();
            }
            return _config.PrimalMangosteen;
        }

        /// <summary>
        /// 获取电能机枪配置
        /// </summary>
        public ElectricGatlingPeaConfig GetElectricGatlingPeaConfig()
        {
            if (_config?.ElectricGatlingPea == null)
            {
                _monitor.Log("电能机枪配置未加载，返回默认配置", LogLevel.Warn);
                return new ElectricGatlingPeaConfig();
            }
            return _config.ElectricGatlingPea;
        }
    }
}
