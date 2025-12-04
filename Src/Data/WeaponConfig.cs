/*
 * Stardew PvZ Weapons Mod
 * 模块: Data
 * 用途: 武器配置数据模型
 */

namespace StardewPvZWeapons.Data
{
    /// <summary>
    /// 武器配置文件根对象
    /// </summary>
    public class WeaponsConfigFile
    {
        public PrimalMangosteenConfig? PrimalMangosteen { get; set; }
        public ElectricGatlingPeaConfig? ElectricGatlingPea { get; set; }
        
        // 未来可以添加更多武器配置
        // public PeashooterConfig? Peashooter { get; set; }
        // public IceWatermelonConfig? IceWatermelon { get; set; }
    }

    /// <summary>
    /// 聚能山竹配置
    /// </summary>
    public class PrimalMangosteenConfig
    {
        /// <summary>
        /// 普通攻击基础伤害
        /// </summary>
        public int BaseDamage { get; set; } = 70;

        /// <summary>
        /// 普通攻击范围（格数）
        /// </summary>
        public int AttackRange { get; set; } = 3;

        /// <summary>
        /// 普通攻击冷却时间（秒）
        /// </summary>
        public float CooldownTime { get; set; } = 2.0f;

        /// <summary>
        /// 麻痹几率（0-1）
        /// </summary>
        public float ParalyzeChance { get; set; } = 1.0f;

        /// <summary>
        /// 麻痹持续时间（秒）
        /// </summary>
        public float ParalyzeDuration { get; set; } = 1.0f;

        /// <summary>
        /// 电圈特效配置
        /// </summary>
        public ElectricRingConfig? ElectricRing { get; set; }

        /// <summary>
        /// 大招配置
        /// </summary>
        public UltimateAbilityConfig? UltimateAbility { get; set; }
    }

    /// <summary>
    /// 电圈特效配置
    /// </summary>
    public class ElectricRingConfig
    {
        /// <summary>
        /// 扩展阶段持续时间（秒）
        /// </summary>
        public float ExpandDuration { get; set; } = 0.25f;

        /// <summary>
        /// 维持阶段持续时间（秒）
        /// </summary>
        public float SustainDuration { get; set; } = 0.1f;

        /// <summary>
        /// 消散阶段持续时间（秒）
        /// </summary>
        public float FadeDuration { get; set; } = 0.15f;

        /// <summary>
        /// 伤害检测间隔（秒）
        /// </summary>
        public float DamageCheckInterval { get; set; } = 0.05f;
    }

    /// <summary>
    /// 大招配置
    /// </summary>
    public class UltimateAbilityConfig
    {
        /// <summary>
        /// 大招范围（格数）
        /// </summary>
        public int Range { get; set; } = 5;

        /// <summary>
        /// 大招伤害
        /// </summary>
        public int Damage { get; set; } = 200;

        /// <summary>
        /// 大招冷却时间（秒）
        /// </summary>
        public float Cooldown { get; set; } = 60.0f;
    }

    /// <summary>
    /// 电能超级机枪射手配置
    /// </summary>
    public class ElectricGatlingPeaConfig
    {
        /// <summary>
        /// 基础伤害
        /// </summary>
        public int BaseDamage { get; set; } = 50;

        /// <summary>
        /// 投射物速度
        /// </summary>
        public int ProjectileSpeed { get; set; } = 12;

        /// <summary>
        /// 射速（每秒发射次数）
        /// </summary>
        public float FireRate { get; set; } = 2f;

        /// <summary>
        /// 每次发射子弹数量
        /// </summary>
        public int ProjectilesPerShot { get; set; } = 4;

        /// <summary>
        /// 攻击范围（格数）
        /// </summary>
        public int AttackRange { get; set; } = 15;

        /// <summary>
        /// 大招触发概率（0-1）
        /// </summary>
        public float UltimateTriggerChance { get; set; } = 0.3f;

        /// <summary>
        /// 大招子弹数量
        /// </summary>
        public int UltimateProjectileCount { get; set; } = 210;

        /// <summary>
        /// 大招持续时间（秒）
        /// </summary>
        public float UltimateDuration { get; set; } = 1.5f;

        /// <summary>
        /// 散射角度（度）
        /// </summary>
        public float SpreadAngle { get; set; } = 15f;

        /// <summary>
        /// 普攻子弹间隔（秒）
        /// </summary>
        public float BulletInterval { get; set; } = 0.1f;

        /// <summary>
        /// 大招伤害倍率
        /// </summary>
        public float UltimateDamageMultiplier { get; set; } = 1.5f;

        /// <summary>
        /// 眩晕持续时间（毫秒）
        /// </summary>
        public float StunDuration { get; set; } = 1500f;
    }
}
