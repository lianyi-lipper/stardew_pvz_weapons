/*
 * Stardew PvZ Weapons Mod
 * 模块: Domain / Weapons
 * 用途: 电能超级机枪射手弹弓
 */

namespace StardewPvZWeapons.Domain.Weapons
{
    using StardewValley;
    using StardewValley.Projectiles;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 电能超级机枪射手弹弓
    /// 一次发射4枚穿透电能子弹，30%概率触发大招散射
    /// </summary>
    public class ElectricGatlingPeaSlingshot : BasePvZSlingshot
    {
        // 配置参数（后续可从 weapons_config.json 读取）
        private const int ElectricDamage = 50;
        private const int ProjectileSpeed = 12;
        private const float FireRate = 2f; // 每秒2次齐射
        private const int ProjectilesPerShot = 4; // 每次受射4枚
        private const float UltimateTriggerChance = 0.3f; // 30%概率触发大招
        private const int UltimateProjectileCount = 210; // 大招发射210枚
        private const float UltimateDuration = 1.5f; // 大招持续1.5秒
        private const float SpreadAngle = 15f; // 子弹扩散角度
        private const float BulletInterval = 0.1f; // 普攻子弹间隔（秒）
        private const float UltimateBulletInterval = UltimateDuration / UltimateProjectileCount; // 大招子弹间隔（约1.5秒/210 ≈ 0.00714秒）

        private Random _random;
        
        // 延迟发射队列
        private struct DelayedShot
        {
            public Farmer Who;
            public GameLocation Location;
            public Vector2 Direction;
            public int Damage;
            public float Timer;
        }
        private Queue<DelayedShot> _shotQueue = new Queue<DelayedShot>();
        private float _burstTimer = 0f; // 连发计时器

        public ElectricGatlingPeaSlingshot() 
            : base("lianyi-lipper.StardewPvZWeapons_ElectricGatlingPea", "电能超级机枪射手", ElectricDamage, "electric_pea")
        {
            _projectileSpeed = ProjectileSpeed;
            _fireRate = FireRate;
            _attackInterval = 1f / _fireRate;
            _piercingShot = true; // 电能子弹可穿透
            AttackRange = 15;
            _random = new Random();

            StardewPvZWeapons.ModEntry.Instance?.Monitor.Log(
                "ElectricGatlingPeaSlingshot created", 
                StardewModdingAPI.LogLevel.Debug);
        }

        /// <summary>
        /// 普通攻击：发射4枚电能子弹
        /// </summary>
        public override void OnAttack(Farmer who, GameLocation location)
        {
            if (_attackCooldown > 0) return;

            // 防止在大招发射期间继续攻击导致队列堆积
            // 如果队列中还有超过10颗子弹，说明大招正在进行，阻止新的攻击
            if (_shotQueue.Count > 10)
            {
                return;
            }

            // 计算发射方向
            Vector2 mousePos = Game1.getMousePosition().ToVector2();
            Vector2 viewport = new Vector2(Game1.viewport.X, Game1.viewport.Y);
            Vector2 worldMousePos = mousePos + viewport;
            Vector2 playerPos = who.getStandingPosition();
            Vector2 direction = worldMousePos - playerPos;
            
            if (direction.LengthSquared() > 0)
            {
                direction.Normalize();
            }
            else
            {
                direction = new Vector2(1, 0);
            }

            // 30% 概率触发大招
            bool triggeredUltimate = _random.NextDouble() < UltimateTriggerChance;

            if (triggeredUltimate)
            {
                // 触发大招：发射大量散射子弹
                FireUltimateBarrage(who, location, direction);
                
                // 显示提示
                Game1.addHUDMessage(new HUDMessage("⚡ 电能爆发！", 2));
                location.playSound("thunder_small");
            }
            else
            {
                // 普通攻击：发射4枚子弹（分批延迟）
                FireNormalShot(who, location, direction);
            }

            // 重置冷却
            _attackCooldown = _attackInterval;
        }

        /// <summary>
        /// 发射普通齐射（4枚子弹）- 延迟发射模式
        /// </summary>
        private void FireNormalShot(Farmer who, GameLocation location, Vector2 direction)
        {
            // 将4枚子弹加入延迟发射队列，每隔0.1秒发射一枚
            for (int i = 0; i < ProjectilesPerShot; i++)
            {
                _shotQueue.Enqueue(new DelayedShot
                {
                    Who = who,
                    Location = location,
                    Direction = direction,
                    Damage = BaseDamage,
                    Timer = i * BulletInterval // 0秒, 0.1秒, 0.2秒, 0.3秒
                });
            }
            
            // 重置连发计时器
            _burstTimer = 0f;
        }

        /// <summary>
        /// 发射大招齐射（210枚散射子弹）- 延迟发射模式
        /// </summary>
        private void FireUltimateBarrage(Farmer who, GameLocation location, Vector2 direction)
        {
            // 将210枚子弹加入延迟发射队列，3秒内发射完毕
            for (int i = 0; i < UltimateProjectileCount; i++)
            {
                // 随机散射角度：-15° 到 +15° (共30°)
                float angle = -15f + (float)(_random.NextDouble() * 30f);
                Vector2 spreadDirection = RotateVector(direction, angle);
                
                // 大招子弹伤害提升 50%
                int ultimateDamage = (int)(BaseDamage * 1.5f);
                
                _shotQueue.Enqueue(new DelayedShot
                {
                    Who = who,
                    Location = location,
                    Direction = spreadDirection,
                    Damage = ultimateDamage,
                    Timer = i * UltimateBulletInterval // 0秒, 0.0143秒, 0.0286秒... 直到3秒
                });
            }
            
            // 重置连发计时器
            _burstTimer = 0f;

            // 添加闪光效果
            Game1.flashAlpha = 0.3f;
        }

        /// <summary>
        /// 创建单个电能豌豆投射物
        /// </summary>
        private void CreateElectricPea(Farmer who, GameLocation location, Vector2 direction, int damage)
        {
            Vector2 startPosition = who.getStandingPosition() + new Vector2(0, -32);
            float velocity = _projectileSpeed;

            // 使用带眩晕效果的电能穿透投射物
            ElectricPiercingProjectile pea = new ElectricPiercingProjectile(
                damage: damage,
                spriteIndex: 10,  // 使用石头外观
                startPosition: startPosition,
                xVelocity: direction.X * velocity,
                yVelocity: direction.Y * velocity,
                location: location,
                firer: who,
                maxDistance: AttackRange * Game1.tileSize,
                stunDuration: 1500f,  // 眩晕1.5秒
                destructiveMode: DestructiveMode
            );

            location.projectiles.Add(pea);
        }

        /// <summary>
        /// 特殊攻击（右键大招）
        /// 由于已经有30%概率自动触发，右键可以作为强制触发
        /// </summary>
        public override bool OnSpecialAttack(Farmer who, GameLocation location)
        {
            // 防止在大招发射期间重复触发
            if (_shotQueue.Count > 10)
            {
                return false;
            }

            // 计算发射方向
            Vector2 mousePos = Game1.getMousePosition().ToVector2();
            Vector2 viewport = new Vector2(Game1.viewport.X, Game1.viewport.Y);
            Vector2 worldMousePos = mousePos + viewport;
            Vector2 playerPos = who.getStandingPosition();
            Vector2 direction = worldMousePos - playerPos;
            
            if (direction.LengthSquared() > 0)
            {
                direction.Normalize();
            }
            else
            {
                direction = new Vector2(1, 0);
            }

            // 强制触发大招散射
            FireUltimateBarrage(who, location, direction);
            
            // 显示提示
            Game1.addHUDMessage(new HUDMessage("⚡⚡ 超级电能爆发！", 1));
            location.playSound("thunder");

            return true;
        }

        protected override int GetProjectileSpriteIndex()
        {
            return 10; // 使用石头外观，后续可自定义为蓝色电能豌豆
        }

        /// <summary>
        /// 重写Update方法，处理延迟发射队列
        /// </summary>
        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            // 处理延迟发射队列
            if (_shotQueue.Count > 0)
            {
                _burstTimer += deltaTime;

                // 检查队列中是否有到时间该发射的子弹
                while (_shotQueue.Count > 0)
                {
                    DelayedShot shot = _shotQueue.Peek();
                    
                    if (_burstTimer >= shot.Timer)
                    {
                        // 时间到了，发射这枚子弹
                        _shotQueue.Dequeue();
                        CreateElectricPea(shot.Who, shot.Location, shot.Direction, shot.Damage);
                        
                        // 播放发射音效
                        if (shot.Location != null)
                        {
                            shot.Location.playSound("Cowboy_gunshot");
                        }
                    }
                    else
                    {
                        // 下一枚子弹还没到时间，退出循环
                        break;
                    }
                }
            }
        }
    }
}
