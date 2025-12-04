/*
 * Stardew PvZ Weapons Mod
 * 模块: Domain / Weapons
 * 用途: 电能穿透投射物类（带眩晕效果）
 */

namespace StardewPvZWeapons.Domain.Weapons
{
    using StardewValley;
    using StardewValley.Projectiles;
    using StardewValley.Monsters;
    using Microsoft.Xna.Framework;
    using System;

    /// <summary>
    /// 电能穿透投射物 - 可以穿透敌人和障碍物，并且眩晕敌人
    /// </summary>
    public class ElectricPiercingProjectile : PiercingProjectile
    {
        private float _stunDuration; // 眩晕持续时间（毫秒）

        public ElectricPiercingProjectile(
            int damage,
            int spriteIndex,
            Vector2 startPosition,
            float xVelocity,
            float yVelocity,
            GameLocation location,
            Character firer,
            float maxDistance,
            float stunDuration = 1000f,  // 默认眩晕1秒
            bool destructiveMode = false
        ) : base(
            damage,
            spriteIndex,
            startPosition,
            xVelocity,
            yVelocity,
            location,
            firer,
            maxDistance,
            destructiveMode
        )
        {
            _stunDuration = stunDuration;
        }

        public override void behaviorOnCollisionWithMonster(NPC n, GameLocation location)
        {
            // 造成伤害
            if (n is Monster monster)
            {
                location.damageMonster(
                    areaOfEffect: monster.GetBoundingBox(),
                    minDamage: base.GetDamage(),
                    maxDamage: base.GetDamage(),
                    isBomb: false,
                    who: this.theOneWhoFiredMe.Get(location) as Farmer
                );

                // ⚡ 眩晕效果（不是击退）
                monster.stunTime.Value = (int)_stunDuration;
                
                // 可选：添加视觉效果（星星绕头）
                // monster.addedSpeed = 0; // 停止移动
            }

            // 不调用 base.base，这样投射物不会被销毁
            // 继续飞行，实现穿透
        }
    }
}
