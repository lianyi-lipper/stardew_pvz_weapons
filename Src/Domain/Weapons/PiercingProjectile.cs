/*
 * Stardew PvZ Weapons Mod
 * 模块: Domain / Weapons
 * 用途: 穿透投射物类
 */

namespace StardewPvZWeapons.Domain.Weapons
{
    using StardewValley;
    using StardewValley.Projectiles;
    using StardewValley.Monsters;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 穿透投射物 - 可以穿透敌人和障碍物
    /// </summary>
    public class PiercingProjectile : BasicProjectile
    {
        private int _damage;
        private float _traveledDistance = 0f;
        private float _maxDistance;
        private Vector2 _lastPosition;
        private bool _destructiveMode; // 是否摧毁障碍物模式
        
        // 记录已击中的树木位置（静态，所有投射物共享）
        private static HashSet<Vector2> _hitTrees = new HashSet<Vector2>();

        public PiercingProjectile(
            int damage,
            int spriteIndex,
            Vector2 startPosition,
            float xVelocity,
            float yVelocity,
            GameLocation location,
            Character firer,
            float maxDistance,
            bool destructiveMode = false
        ) : base(
            damage,
            spriteIndex,
            0, // bouncesTillDestruct = 0
            0, // tailLength
            0f, // rotationVelocity
            xVelocity,
            yVelocity,
            startPosition,
            "", "", "", // sounds
            false, // explode
            true, // damagesMonsters
            location,
            firer,
            null, // collisionBehavior
            null  // shotItemId
        )
        {
            _damage = damage;
            _maxDistance = maxDistance;
            _lastPosition = startPosition;
            _destructiveMode = destructiveMode;

            // 设置穿透属性
            this.ignoreTravelGracePeriod.Value = true;
            this.IgnoreLocationCollision = true;
            this.ignoreMeleeAttacks.Value = true;
        }

        /// <summary>
        /// 获取伤害值（供子类使用）
        /// </summary>
        protected int GetDamage()
        {
            return _damage;
        }

        public override void behaviorOnCollisionWithMonster(NPC n, GameLocation location)
        {
            // 造成伤害但不消失
            if (n is Monster monster)
            {
                location.damageMonster(
                    areaOfEffect: monster.GetBoundingBox(),
                    minDamage: _damage,
                    maxDamage: _damage,
                    isBomb: false,
                    who: this.theOneWhoFiredMe.Get(location) as Farmer
                );
            }

            // 不调用 base，这样投射物不会被销毁
            // 继续飞行，实现穿透
        }

        /// <summary>
        /// 重写地形特征碰撞（树木等）- 可选择穿透或摧毁
        /// </summary>
        public override void behaviorOnCollisionWithTerrainFeature(StardewValley.TerrainFeatures.TerrainFeature t, Vector2 tileLocation, GameLocation location)
        {
            if (!_destructiveMode)
            {
                // 穿透模式：什么都不做
                // 不调用 base，不执行 piercesLeft--，不播放动画
                // 投射物直接穿过树木继续飞行
                return;
            }

            // 破坏模式：摧毁障碍物
            var farmer = this.theOneWhoFiredMe.Get(location) as Farmer;
            
            if (t is StardewValley.TerrainFeatures.Tree tree)
            {
                // 如果树木正在倒下，不处理（等待动画完成）
                if (tree.falling.Value)
                {
                    return;
                }
                
                // 如果这个投射物已经击中过这个位置的树木（且不是树桩），不再处理
                // 注意：树桩应该可以被击中，所以不检查树桩
                if (!tree.stump.Value && _hitTrees.Contains(tileLocation))
                {
                    return;
                }
                
                // 第一次攻击：触发树木倒下（使用游戏原版方法）
                if (!tree.stump.Value && tree.growthStage.Value >= 5)
                {
                    // 记录已击中此位置
                    _hitTrees.Add(tileLocation);
                    
                    // 调用游戏原版的 instantDestroy 方法
                    // 这会正确设置：stump=true, health=5, falling=true
                    // 并播放倒下动画，动画结束后留下树桩
                    tree.instantDestroy(tileLocation);
                }
                else
                {
                    // 树桩/小树/树苗的处理
                    // 使用游戏原生方法销毁，会根据生长阶段自动处理掉落
                    tree.instantDestroy(tileLocation);
                    location.terrainFeatures.Remove(tileLocation);
                }
            }
            else if (t is StardewValley.TerrainFeatures.Grass grass)
            {
                // 直接移除草
                location.terrainFeatures.Remove(tileLocation);
                location.playSound("cut");
            }
            else if (t is StardewValley.TerrainFeatures.Bush bush)
            {
                // 移除灌木
                location.terrainFeatures.Remove(tileLocation);
                location.playSound("leafrustle");
            }
        }


        public override bool update(GameTime time, GameLocation location)
        {
            // 计算飞行距离
            Vector2 currentPos = this.position.Value;
            _traveledDistance += Vector2.Distance(_lastPosition, currentPos);
            _lastPosition = currentPos;

            // 超过最大距离则销毁
            if (_traveledDistance >= _maxDistance)
            {
                return true; // 返回 true 表示销毁
            }

            // 🔥 如果破坏模式开启，主动检测并摧毁石头等对象
            if (_destructiveMode)
            {
                Vector2 tileLocation = new Vector2((int)(currentPos.X / 64f), (int)(currentPos.Y / 64f));
                var farmer = this.theOneWhoFiredMe.Get(location) as Farmer;
                
                // 首先检查是否有大型资源（大石头、大树桩等）
                foreach (var clump in location.resourceClumps)
                {
                    // 检查投射物是否击中这个资源块
                    var clumpRect = new Microsoft.Xna.Framework.Rectangle(
                        (int)clump.Tile.X * 64, 
                        (int)clump.Tile.Y * 64,
                        clump.width.Value * 64, 
                        clump.height.Value * 64
                    );
                    
                    if (clumpRect.Contains((int)currentPos.X, (int)currentPos.Y))
                    {
                        // 使用游戏原生方法摧毁资源块（自动处理掉落）
                        var tool = clump.parentSheetIndex.Value >= 672 ? 
                            (StardewValley.Tool)new StardewValley.Tools.Pickaxe() : 
                            (StardewValley.Tool)new StardewValley.Tools.Axe();
                        tool.lastUser = farmer;
                        
                        if (clump.destroy(tool, location, clump.Tile))
                        {
                            location.resourceClumps.Remove(clump);
                            goto destroyDone;
                        }
                    }
                }
                
                // 检查普通对象（小石头、杂草、树枝等）
                if (location.objects.ContainsKey(tileLocation))
                {
                    var obj = location.objects[tileLocation];
                    
                    if (obj.IsBreakableStone())
                    {
                        // 使用游戏原生方法处理石头破坏（包含正确的掉落逻辑）
                        location.OnStoneDestroyed(obj.ItemId, (int)tileLocation.X, (int)tileLocation.Y, farmer);
                        location.objects.Remove(tileLocation);
                        location.playSound("stoneCrack", tileLocation);
                        
                        // 统计数据
                        if (farmer != null)
                        {
                            Game1.stats.RocksCrushed++;
                        }
                        goto destroyDone;
                    }
                    else if (obj.IsWeeds())
                    {
                        // 使用游戏原生方法处理杂草（包含掉落）
                        obj.cutWeed(farmer);
                        location.objects.Remove(tileLocation);
                        goto destroyDone;
                    }
                    else if (obj.IsTwig())
                    {
                        // 创建临时斧头并调用游戏方法
                        var axe = new StardewValley.Tools.Axe();
                        axe.lastUser = farmer;
                        
                        if (obj.performToolAction(axe))
                        {
                            location.objects.Remove(tileLocation);
                            goto destroyDone;
                        }
                    }
                }
                
                destroyDone:; // 标签用于跳出循环
            }

            // 调用基类更新
            return base.update(time, location);
        }
    }
}
