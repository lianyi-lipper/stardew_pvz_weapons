/*
 * Stardew PvZ Weapons Mod
 * 模块: Domain / Effects
 * 用途: 聚能山竹饰品效果实现
 */

namespace StardewPvZWeapons.Domain.Effects
{
    using System;
    using System.Linq;
    using Microsoft.Xna.Framework;
    using StardewValley;
    using StardewValley.Objects.Trinkets;
    using StardewValley.Monsters;
    using StardewPvZWeapons.Services;

    /// <summary>
    /// 聚能山竹饰品效果
    /// 实现自动攻击和大招逻辑
    /// </summary>
    public class PrimalMangosteenEffect : TrinketEffect
    {
        // 配置数据（从 weapons_config.json 加载）
        private Data.PrimalMangosteenConfig _config;

        // 衍生参数
        private float _autoAttackRadius; // 根据AttackRange计算（1格 = 64像素）
        
        // 配置初始化标志
        private bool _isConfigured = false;

        /// <summary>
        /// 普通攻击冷却计时器
        /// </summary>
        private float _attackCooldownTimer;

        /// <summary>
        /// 大招冷却计时器
        /// </summary>
        private float _ultimateCooldownTimer;

        /// <summary>
        /// 大招是否已就绪
        /// </summary>
        public bool IsUltimateReady => _ultimateCooldownTimer <= 0;

        /// <summary>
        /// 玩家头顶的悬浮山竹图标
        /// </summary>
        private StardewValley.TemporaryAnimatedSprite? _floatingIcon;

        /// <summary>
        /// 默认构造函数（游戏通过反射调用）
        /// </summary>
        public PrimalMangosteenEffect() : base(null!)
        {
            // 使用默认配置（ModEntry 会调用 Initialize 加载实际配置）
            _config = new Data.PrimalMangosteenConfig();
            _autoAttackRadius = _config.AttackRange * 64f;
            
            StardewPvZWeapons.ModEntry.Instance?.Monitor.Log(
                "PrimalMangosteenEffect: Parameterless constructor called", 
                StardewModdingAPI.LogLevel.Trace);
        }

        /// <summary>
        /// 带Trinket参数的构造函数
        /// </summary>
        public PrimalMangosteenEffect(StardewValley.Objects.Trinkets.Trinket trinket) : base(trinket)
        {
            _config = new Data.PrimalMangosteenConfig();
            _autoAttackRadius = _config.AttackRange * 64f;
            
            StardewPvZWeapons.ModEntry.Instance?.Monitor.Log(
                $"PrimalMangosteenEffect: Constructor with Trinket called. Trinket: {trinket?.Name}", 
                StardewModdingAPI.LogLevel.Trace);
        }

        /// <summary>
        /// 初始化配置（由 ModEntry 在创建后调用）
        /// </summary>
        public void Initialize(Data.PrimalMangosteenConfig config)
        {
            if (_isConfigured) return; // 防止重复初始化
            
            _config = config;
            _autoAttackRadius = config.AttackRange * 64f;
            _isConfigured = true;

            StardewPvZWeapons.ModEntry.Instance?.Monitor.Log(
                $"PrimalMangosteenEffect 配置已加载: 伤害={config.BaseDamage}, 范围={config.AttackRange}", 
                StardewModdingAPI.LogLevel.Trace);
        }

        /// <summary>
        /// 每帧更新（需要在ModEntry中手动调用）
        /// </summary>
        public void Update(Farmer farmer)
        {
            if (farmer == null || farmer.currentLocation == null) return;

            // 更新冷却（假设60FPS）
            float deltaTime = 1f / 60f;
            UpdateCooldowns(deltaTime);

            // 更新或创建悬浮山竹图标
            UpdateFloatingIcon(farmer);

            // 自动攻击附近敌人
            AutoAttackNearbyEnemies(farmer, farmer.currentLocation);
        }

        /// <summary>
        /// 更新冷却状态
        /// </summary>
        private void UpdateCooldowns(float deltaTime)
        {
            if (_attackCooldownTimer > 0)
            {
                _attackCooldownTimer -= deltaTime;
                if (_attackCooldownTimer < 0) _attackCooldownTimer = 0;
            }

            if (_ultimateCooldownTimer > 0)
            {
                _ultimateCooldownTimer -= deltaTime;
                if (_ultimateCooldownTimer < 0) _ultimateCooldownTimer = 0;
            }
        }

        /// <summary>
        /// 更新玩家头顶的悬浮山竹图标
        /// </summary>
        private void UpdateFloatingIcon(Farmer farmer)
        {
            var location = farmer.currentLocation;
            
            // 检查图标是否仍在当前场景中（防止切换场景后丢失）
            bool iconExistsInLocation = _floatingIcon != null && 
                                        location.temporarySprites.Contains(_floatingIcon);
            
            // 如果图标不存在、已消失或不在当前场景，创建新的
            if (_floatingIcon == null || _floatingIcon.alpha <= 0 || !iconExistsInLocation)
            {
                // 计算位置：玩家头顶上方（玩家高度约64像素，再向上20像素）
                var iconPosition = farmer.Position + new Microsoft.Xna.Framework.Vector2(0, -84);
                
                _floatingIcon = new StardewValley.TemporaryAnimatedSprite(
                    "Mods/lianyi-lipper.StardewPvZWeapons/Trinkets",
                    new Microsoft.Xna.Framework.Rectangle(0, 0, 16, 16), // 饰品图标尺寸
                    9999f, // 持续时间（非常长，手动管理）
                    1,
                    999,
                    iconPosition,
                    false,
                    false
                )
                {
                    scale = 2f, // 放大2倍
                    alpha = 0.8f,
                    layerDepth = 1f,
                    yPeriodic = true, // 上下浮动
                    yPeriodicLoopTime = 2000f, // 2秒一个周期
                    yPeriodicRange = 8f // 浮动范围8像素
                };
                
                location.temporarySprites.Add(_floatingIcon);
            }
            else
            {
                // 更新位置跟随玩家
                _floatingIcon.position = farmer.Position + new Microsoft.Xna.Framework.Vector2(0, -84);
            }
        }

        /// <summary>
        /// 自动攻击附近的敌人
        /// </summary>
        private void AutoAttackNearbyEnemies(Farmer who, GameLocation location)
        {
            // 检查冷却
            if (_attackCooldownTimer > 0) 
            {
                // StardewPvZWeapons.ModEntry.Instance?.Monitor.Log($"Attack Cooldown: {_attackCooldownTimer}", StardewModdingAPI.LogLevel.Trace);
                return;
            }

            // 检查附近是否有敌人
            bool foundEnemy = false;
            foreach (var character in location.characters)
            {
                if (character is Monster monster && monster.Health > 0)
                {
                    float distance = Vector2.Distance(who.Position, monster.Position);
                    // StardewPvZWeapons.ModEntry.Instance?.Monitor.Log($"Found monster: {monster.Name} at distance {distance} (Range: {AutoAttackRadius})", StardewModdingAPI.LogLevel.Trace);
                    
                    if (distance <= _autoAttackRadius)
                    {
                        foundEnemy = true;
                        StardewPvZWeapons.ModEntry.Instance?.Monitor.Log($"Target acquired: {monster.Name} at distance {distance}", StardewModdingAPI.LogLevel.Trace);
                        break;
                    }
                }
            }

            // 如果发现敌人，释放电圈
            if (foundEnemy)
            {
                PerformAttack(who, location);
            }
        }

        /// <summary>
        /// 执行普通攻击
        /// </summary>
        private void PerformAttack(Farmer who, GameLocation location)
        {
            // 检查冷却
            if (_attackCooldownTimer > 0) return;

            // 在玩家位置生成3x3电圈
            var playerCenter = who.Position + new Vector2(32, 32);
            EffectManager.Instance.CreateElectricRing(
                center: playerCenter,
                maxRange: _config.AttackRange,
                damage: _config.BaseDamage,
                paralyzeDuration: _config.ParalyzeDuration,
                location: location
            );

            // 触发冷却
            _attackCooldownTimer = _config.CooldownTime;

            // 播放音效
            location.playSound("thunder_small");
        }

        /// <summary>
        /// 执行特殊攻击（大招）
        /// 由ModEntry响应C键调用
        /// </summary>
        public bool PerformUltimate(Farmer who, GameLocation location)
        {
            // 检查冷却
            if (_ultimateCooldownTimer > 0)
            {
                int remainingSeconds = (int)Math.Ceiling(_ultimateCooldownTimer);
                Game1.addHUDMessage(new HUDMessage(string.Format(StardewPvZWeapons.ModEntry.Instance?.Helper.Translation.Get("weapon.primal.cooldown.message") ?? "Cooling down: {0}s", remainingSeconds), 3));
                return false;
            }

            // 在玩家位置生成5x5巨型电圈
            var playerCenter = who.Position + new Vector2(32, 32);
            
            // 播放闪电音效
            location.playSound("thunder");

            // 创建垂直闪电特效
            var lightningTexture = StardewPvZWeapons.ModEntry.LightningTexture;
            if (lightningTexture != null)
            {
                // 使用基础构造函数并手动设置纹理
                var lightningSprite = new StardewValley.TemporaryAnimatedSprite(
                    "LooseSprites\\Cursors", // 占位路径
                    new Microsoft.Xna.Framework.Rectangle(0, 0, 32, 128),
                    150f, // 持续时间
                    1, // 帧数
                    0, // 循环次数（0=只播放一次）
                    playerCenter + new Microsoft.Xna.Framework.Vector2(-16, -128),
                    false,
                    false
                )
                {
                    texture = lightningTexture, // 手动设置纹理
                    scale = 1f,
                    color = Microsoft.Xna.Framework.Color.Purple, // 紫色主题
                    alpha = 1f,
                    alphaFade = 0.01f,
                    layerDepth = 1f
                };
                location.temporarySprites.Add(lightningSprite);
            }

            // 屏幕闪光效果
            Game1.flashAlpha = 0.5f;

            // 生成大型电圈
            EffectManager.Instance.CreateElectricRing(
                center: playerCenter,
                maxRange: _config.UltimateAbility != null ? _config.UltimateAbility.Range : 5,
                damage: _config.UltimateAbility != null ? _config.UltimateAbility.Damage : 200,
                paralyzeDuration: _config.ParalyzeDuration,
                location: location
            );

            // 触发冷却
            _ultimateCooldownTimer = _config.UltimateAbility != null ? _config.UltimateAbility.Cooldown : 60.0f;

            // 显示成功提示
            Game1.addHUDMessage(new HUDMessage(StardewPvZWeapons.ModEntry.Instance?.Helper.Translation.Get("weapon.primal.lightning") ?? "Summon Lightning!", 2));

            return true;
        }

        /// <summary>
        /// 获取额外描述信息（大招状态）
        /// </summary>
        public string GetExtraDescription()
        {
            var translation = StardewPvZWeapons.ModEntry.Instance?.Helper.Translation;
            if (IsUltimateReady)
            {
                return translation?.Get("weapon.primal.extra.ready") ?? "\n\nPress C: Summon Lightning (Ready!)";
            }
            else
            {
                int remainingSeconds = (int)Math.Ceiling(_ultimateCooldownTimer);
                return string.Format(translation?.Get("weapon.primal.extra.cooldown") ?? "\n\nPress C: Summon Lightning (CD: {0}s)", remainingSeconds);
            }
        }
    }
}
