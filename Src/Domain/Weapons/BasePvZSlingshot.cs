/*
 * Stardew PvZ Weapons Mod
 * 模块: Domain / Weapons
 * 用途: PvZ 弹弓武器基类
 */

namespace StardewPvZWeapons.Domain.Weapons
{
    using StardewValley.Tools;
    using StardewValley;
    using StardewValley.Projectiles;
    using Microsoft.Xna.Framework;
    using System;
    using System.Xml.Serialization;

    /// <summary>
    /// PvZ 弹弓武器基类
    /// 用于实现投射类植物武器（豌豆射手、椰子炮等）
    /// </summary>
    public class BasePvZSlingshot : Slingshot, ICustomWeapon
    {
        public new virtual string Name { get; protected set; } = "PvZ Slingshot";
        public virtual int BaseDamage { get; protected set; } = 50;
        public virtual int AttackRange { get; protected set; } = 10;

        // 弹药配置
        protected string _projectileType;  // 投射物类型
        protected int _projectileSpeed;    // 投射速度
        protected float _fireRate;         // 射速（每秒发射数）
        protected bool _piercingShot;      // 是否穿透

        // 冷却系统
        protected float _attackCooldown = 0f;
        protected float _attackInterval;   // 攻击间隔（根据射速计算）
        
        // 破坏模式
        public bool DestructiveMode { get; set; } = false;

        public BasePvZSlingshot(string itemId, string name, int damage, string projectileType)
            : base(itemId)
        {
            Name = name;
            BaseDamage = damage;
            _projectileType = projectileType;
            _projectileSpeed = 10;
            _fireRate = 1f;
            _attackInterval = 1f / _fireRate;
            AttackRange = 10;
            _piercingShot = false;
        }

        /// <summary>
        /// 发射投射物
        /// </summary>
        public virtual void OnAttack(Farmer who, GameLocation location)
        {
            if (_attackCooldown > 0) 
            {
                // 冷却中，不能攻击
                return;
            }

            // 计算发射方向（朝向鼠标位置）
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
                // 如果鼠标在玩家位置，默认向右发射
                direction = new Vector2(1, 0);
            }

            // 创建投射物
            CreateProjectile(who, location, direction);

            // 重置冷却
            _attackCooldown = _attackInterval;
        }

        /// <summary>
        /// 创建自定义投射物
        /// 子类可以重写此方法实现自定义投射物
        /// </summary>
        protected virtual void CreateProjectile(Farmer who, GameLocation location, Vector2 direction)
        {
            Vector2 startPosition = who.getStandingPosition() + new Vector2(0, -32);
            float velocity = _projectileSpeed;

            // 使用自定义穿透投射物
            PiercingProjectile projectile = new PiercingProjectile(
                damage: BaseDamage,
                spriteIndex: GetProjectileSpriteIndex(),
                startPosition: startPosition,
                xVelocity: direction.X * velocity,
                yVelocity: direction.Y * velocity,
                location: location,
                firer: who,
                maxDistance: AttackRange * Game1.tileSize,
                destructiveMode: DestructiveMode
            );
            
            location.projectiles.Add(projectile);
        }

        /// <summary>
        /// 获取投射物的精灵索引
        /// 子类可以重写以使用不同的外观
        /// </summary>
        protected virtual int GetProjectileSpriteIndex()
        {
            return 10; // 使用物品精灵索引 10 (石头)，后续可自定义
        }

        /// <summary>
        /// 特殊攻击（大招）
        /// </summary>
        public virtual bool OnSpecialAttack(Farmer who, GameLocation location)
        {
            // 子类实现具体大招逻辑
            return false;
        }

        /// <summary>
        /// 每帧更新
        /// </summary>
        public virtual void Update(float deltaTime)
        {
            if (_attackCooldown > 0)
            {
                _attackCooldown -= deltaTime;
                if (_attackCooldown < 0) _attackCooldown = 0;
            }
        }

        /// <summary>
        /// 旋转向量（用于扇形散射）
        /// </summary>
        protected Vector2 RotateVector(Vector2 v, float degrees)
        {
            float radians = MathHelper.ToRadians(degrees);
            float cos = (float)Math.Cos(radians);
            float sin = (float)Math.Sin(radians);
            return new Vector2(
                v.X * cos - v.Y * sin,
                v.X * sin + v.Y * cos
            );
        }
    }
}
