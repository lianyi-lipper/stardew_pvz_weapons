/*
 * Stardew PvZ Weapons Mod
 * 模块: Domain / Weapons
 * 用途: 自定义武器接口定义
 */

namespace StardewPvZWeapons.Domain.Weapons
{
    using StardewValley;
    using Microsoft.Xna.Framework;

    /// <summary>
    /// 自定义武器接口
    /// 定义所有自定义武器必须实现的基础功能
    /// </summary>
    public interface ICustomWeapon
    {
        /// <summary>
        /// 武器名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 基础伤害值
        /// </summary>
        int BaseDamage { get; }

        /// <summary>
        /// 攻击范围（以格子为单位）
        /// </summary>
        int AttackRange { get; }

        /// <summary>
        /// 执行普通攻击
        /// </summary>
        /// <param name="who">使用武器的玩家</param>
        /// <param name="location">当前游戏位置</param>
        void OnAttack(Farmer who, GameLocation location);

        /// <summary>
        /// 执行特殊攻击（大招）
        /// </summary>
        /// <param name="who">使用武器的玩家</param>
        /// <param name="location">当前游戏位置</param>
        /// <returns>是否成功触发（可能因冷却未就绪而失败）</returns>
        bool OnSpecialAttack(Farmer who, GameLocation location);

        /// <summary>
        /// 每帧更新武器状态（主要用于冷却计时）
        /// </summary>
        /// <param name="deltaTime">距离上一帧的时间间隔（秒）</param>
        void Update(float deltaTime);
    }
}
