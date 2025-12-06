/*
 * Stardew PvZ Weapons Mod
 * 模块: Services
 * 用途: 电能超级机枪射手武器管理器
 */

namespace StardewPvZWeapons.Services
{
    using StardewValley;
    using StardewValley.Tools;
    using StardewValley.Projectiles;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using System;
    using System.Collections.Generic;
    using StardewPvZWeapons.Domain.Weapons;

    /// <summary>
    /// 电能超级机枪射手武器管理器
    /// 负责管理通过 modData 标记的自定义弹弓武器行为
    /// </summary>
    public class ElectricGatlingPeaManager
    {
        // 配置（从 JSON 加载）
        private Data.ElectricGatlingPeaConfig _config = new Data.ElectricGatlingPeaConfig();

        // 自定义子弹贴图
        private Texture2D? _largeBulletTexture;
        private Texture2D? _smallBulletTexture;

        // 延迟发射队列
        private struct DelayedShot
        {
            public Farmer Who;
            public GameLocation Location;
            public Vector2 Direction;      // 普攻用的固定方向
            public float SpreadAngle;      // 大招散射角度偏移
            public bool IsUltimate;        // 是否是大招（大招需要动态跟踪鼠标）
            public int Damage;
            public float Timer;
            public bool UseLargeBullet;    // 是否使用大子弹
        }

        private Dictionary<string, Queue<DelayedShot>> _shotQueues = new Dictionary<string, Queue<DelayedShot>>();
        private Dictionary<string, float> _burstTimers = new Dictionary<string, float>();
        private Dictionary<string, float> _attackCooldowns = new Dictionary<string, float>();
        private Dictionary<string, bool> _destructiveModes = new Dictionary<string, bool>();
        private Random _random = new Random();

        /// <summary>
        /// 初始化管理器，加载配置
        /// </summary>
        public void Initialize(Data.ElectricGatlingPeaConfig config)
        {
            _config = config ?? new Data.ElectricGatlingPeaConfig();
        }

        /// <summary>
        /// 设置自定义子弹贴图
        /// </summary>
        public void SetBulletTextures(Texture2D? largeBullet, Texture2D? smallBullet)
        {
            _largeBulletTexture = largeBullet;
            _smallBulletTexture = smallBullet;
        }

        /// <summary>
        /// 检查是否是电能超级机枪射手
        /// </summary>
        public static bool IsElectricGatlingPea(Tool tool)
        {
            return tool is Slingshot slingshot && 
                   slingshot.modData.ContainsKey("lianyi-lipper.StardewPvZWeapons/WeaponType") &&
                   slingshot.modData["lianyi-lipper.StardewPvZWeapons/WeaponType"] == "ElectricGatlingPea";
        }

        /// <summary>
        /// 获取武器的唯一ID
        /// </summary>
        private string GetWeaponId(Tool weapon)
        {
            return $"{weapon.GetHashCode()}";
        }

        /// <summary>
        /// 获取或创建发射队列
        /// </summary>
        private Queue<DelayedShot> GetShotQueue(string weaponId)
        {
            if (!_shotQueues.ContainsKey(weaponId))
            {
                _shotQueues[weaponId] = new Queue<DelayedShot>();
            }
            return _shotQueues[weaponId];
        }

        /// <summary>
        /// 获取或设置破坏模式
        /// </summary>
        public bool GetDestructiveMode(Tool weapon)
        {
            string id = GetWeaponId(weapon);
            return _destructiveModes.ContainsKey(id) && _destructiveModes[id];
        }

        public void SetDestructiveMode(Tool weapon, bool value)
        {
            string id = GetWeaponId(weapon);
            _destructiveModes[id] = value;
        }

        /// <summary>
        /// 普通政击
        /// </summary>
        public void OnAttack(Farmer who, GameLocation location, Tool weapon)
        {
            string weaponId = GetWeaponId(weapon);
            float attackInterval = 1f / _config.FireRate;

            if (!_attackCooldowns.ContainsKey(weaponId))
            {
                _attackCooldowns[weaponId] = 0f;
            }

            if (_attackCooldowns[weaponId] > 0) return;

            var shotQueue = GetShotQueue(weaponId);
            
            // 防止队列堆积
            if (shotQueue.Count > 10)
            {
                return;
            }

            // 计算发射方向
            Vector2 direction = CalculateDirection(who);

            // 使用配置的大招触发概率
            bool triggeredUltimate = _random.NextDouble() < _config.UltimateTriggerChance;

            if (triggeredUltimate)
            {
                FireUltimateBarrage(who, location, direction, weaponId);
                Game1.addHUDMessage(new HUDMessage(StardewPvZWeapons.ModEntry.Instance?.Helper.Translation.Get("weapon.electric-gatling-pea.ultimate") ?? "Electric Burst!", 2));
                location.playSound("thunder_small");
            }
            else
            {
                FireNormalShot(who, location, direction, weaponId);
            }

            // 重置冷却
            _attackCooldowns[weaponId] = attackInterval;
        }

        /// <summary>
        /// 特殊攻击（右键大招）
        /// </summary>
        public bool OnSpecialAttack(Farmer who, GameLocation location, Tool weapon)
        {
            string weaponId = GetWeaponId(weapon);
            var shotQueue = GetShotQueue(weaponId);

            // 防止重复触发
            if (shotQueue.Count > 10)
            {
                return false;
            }

            Vector2 direction = CalculateDirection(who);
            FireUltimateBarrage(who, location, direction, weaponId);
            
            Game1.addHUDMessage(new HUDMessage(StardewPvZWeapons.ModEntry.Instance?.Helper.Translation.Get("weapon.electric-gatling-pea.super-ultimate") ?? "Super Electric Burst!", 1));
            location.playSound("thunder");

            return true;
        }

        /// <summary>
        /// 更新武器状态
        /// </summary>
        public void Update(float deltaTime, Tool weapon)
        {
            string weaponId = GetWeaponId(weapon);

            // 更新冷却
            if (_attackCooldowns.ContainsKey(weaponId) && _attackCooldowns[weaponId] > 0)
            {
                _attackCooldowns[weaponId] -= deltaTime;
                if (_attackCooldowns[weaponId] < 0) _attackCooldowns[weaponId] = 0;
            }

            // 处理延迟发射队列
            var shotQueue = GetShotQueue(weaponId);
            if (shotQueue.Count > 0)
            {
                if (!_burstTimers.ContainsKey(weaponId))
                {
                    _burstTimers[weaponId] = 0f;
                }

                _burstTimers[weaponId] += deltaTime;

                while (shotQueue.Count > 0)
                {
                    DelayedShot shot = shotQueue.Peek();
                    
                    if (_burstTimers[weaponId] >= shot.Timer)
                    {
                        shotQueue.Dequeue();
                        
                        // 计算发射方向
                        Vector2 direction;
                        if (shot.IsUltimate)
                        {
                            // 大招：重新计算当前鼠标方向，并加上散射角度
                            Vector2 currentDirection = CalculateDirection(shot.Who);
                            direction = RotateVector(currentDirection, shot.SpreadAngle);
                        }
                        else
                        {
                            // 普攻：使用固定方向
                            direction = shot.Direction;
                        }
                        
                        CreateElectricPea(shot.Who, shot.Location, direction, shot.Damage, weapon, shot.UseLargeBullet);
                        
                        if (shot.Location != null)
                        {
                            shot.Location.playSound("Cowboy_gunshot");
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 计算发射方向
        /// </summary>
        private Vector2 CalculateDirection(Farmer who)
        {
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

            return direction;
        }

        /// <summary>
        /// 计算子弹发射起点（和武器贴图位置一致）
        /// </summary>
        private Vector2 CalculateProjectileStartPosition(Farmer who)
        {
            Vector2 playerPos = who.getStandingPosition();
            int facingDirection = who.FacingDirection;
            
            // 使用和 SlingshotTexturePatch.DrawHeldWeapon 相同的 offset，并往下偏移5个单位
            Vector2 offset;
            switch (facingDirection)
            {
                case 0: // 上
                    offset = new Vector2(0, -42);
                    break;
                case 1: // 右
                    offset = new Vector2(30, -42);
                    break;
                case 2: // 下
                    offset = new Vector2(0, -42);
                    break;
                case 3: // 左
                    offset = new Vector2(-30, -42);
                    break;
                default:
                    offset = new Vector2(0, -2);
                    break;
            }
            
            return playerPos + offset;
        }

        /// <summary>
        /// 发射普通齐射
        /// </summary>
        private void FireNormalShot(Farmer who, GameLocation location, Vector2 direction, string weaponId)
        {
            var shotQueue = GetShotQueue(weaponId);

            for (int i = 0; i < _config.ProjectilesPerShot; i++)
            {
                // 普攻只使用小子弹
                shotQueue.Enqueue(new DelayedShot
                {
                    Who = who,
                    Location = location,
                    Direction = direction,
                    Damage = _config.BaseDamage,
                    Timer = i * _config.BulletInterval,
                    UseLargeBullet = false  // 普攻固定使用小子弹
                });
            }
            
            if (!_burstTimers.ContainsKey(weaponId))
            {
                _burstTimers[weaponId] = 0f;
            }
            else
            {
                _burstTimers[weaponId] = 0f;
            }
        }

        /// <summary>
        /// 发射大招齐射
        /// </summary>
        private void FireUltimateBarrage(Farmer who, GameLocation location, Vector2 direction, string weaponId)
        {
            var shotQueue = GetShotQueue(weaponId);
            float ultimateBulletInterval = _config.UltimateDuration / _config.UltimateProjectileCount;

            for (int i = 0; i < _config.UltimateProjectileCount; i++)
            {
                // 只存储散射角度，发射时会根据当前鼠标位置动态计算方向
                float spreadAngle = -_config.SpreadAngle + (float)(_random.NextDouble() * _config.SpreadAngle * 2);
                int ultimateDamage = (int)(_config.BaseDamage * _config.UltimateDamageMultiplier);
                
                // 根据配置的比例随机选择子弹类型
                bool useLarge = _random.NextDouble() < _config.LargeBulletRatio;
                
                shotQueue.Enqueue(new DelayedShot
                {
                    Who = who,
                    Location = location,
                    Direction = Vector2.Zero,  // 大招不使用固定方向
                    SpreadAngle = spreadAngle, // 存储散射角度
                    IsUltimate = true,         // 标记为大招，发射时动态跟踪鼠标
                    Damage = ultimateDamage,
                    Timer = i * ultimateBulletInterval,
                    UseLargeBullet = useLarge
                });
            }
            
            if (!_burstTimers.ContainsKey(weaponId))
            {
                _burstTimers[weaponId] = 0f;
            }
            else
            {
                _burstTimers[weaponId] = 0f;
            }

            Game1.flashAlpha = 0.3f;
        }

        /// <summary>
        /// 创建电能豌豆投射物
        /// </summary>
        private void CreateElectricPea(Farmer who, GameLocation location, Vector2 direction, int damage, Tool weapon, bool useLargeBullet = false)
        {
            // 计算发射起点，和武器贴图位置一致
            Vector2 startPosition = CalculateProjectileStartPosition(who);
            float velocity = _config.ProjectileSpeed;

            // 选择贴图
            Texture2D? bulletTexture = useLargeBullet ? _largeBulletTexture : _smallBulletTexture;

            if (bulletTexture != null)
            {
                // 使用自定义贴图，大子弹缩放4f，小子弹缩放2f
                float scale = useLargeBullet ? 4f : 2f;
                
                var pea = new CustomTexturedElectricProjectile(
                    damage: damage,
                    startPosition: startPosition,
                    xVelocity: direction.X * velocity,
                    yVelocity: direction.Y * velocity,
                    location: location,
                    firer: who,
                    maxDistance: _config.AttackRange * Game1.tileSize,
                    customTexture: bulletTexture,
                    scale: scale,
                    stunDuration: _config.StunDuration,
                    destructiveMode: GetDestructiveMode(weapon)
                );
                location.projectiles.Add(pea);
            }
            else
            {
                // 回退到原版投射物
                ElectricPiercingProjectile pea = new ElectricPiercingProjectile(
                    damage: damage,
                    spriteIndex: 10,
                    startPosition: startPosition,
                    xVelocity: direction.X * velocity,
                    yVelocity: direction.Y * velocity,
                    location: location,
                    firer: who,
                    maxDistance: _config.AttackRange * Game1.tileSize,
                    stunDuration: _config.StunDuration,
                    destructiveMode: GetDestructiveMode(weapon)
                );
                location.projectiles.Add(pea);
            }
        }

        /// <summary>
        /// 旋转向量
        /// </summary>
        private Vector2 RotateVector(Vector2 v, float degrees)
        {
            float radians = MathHelper.ToRadians(degrees);
            float cos = (float)Math.Cos(radians);
            float sin = (float)Math.Sin(radians);
            return new Vector2(
                v.X * cos - v.Y * sin,
                v.X * sin + v.Y * cos
            );
        }

        /// <summary>
        /// 清理武器数据
        /// </summary>
        public void CleanupWeapon(Tool weapon)
        {
            string weaponId = GetWeaponId(weapon);
            _shotQueues.Remove(weaponId);
            _burstTimers.Remove(weaponId);
            _attackCooldowns.Remove(weaponId);
            _destructiveModes.Remove(weaponId);
        }
    }
}
