/*
 * Stardew PvZ Weapons Mod
 * 模块: Domain / Effects
 * 用途: 电圈AOE特效实现
 */

namespace StardewPvZWeapons.Domain.Effects
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Xna.Framework;
    using StardewValley;
    using StardewValley.Monsters;

    /// <summary>
    /// 电圈特效
    /// 从中心向外扩散的范围攻击效果
    /// </summary>
    public class ElectricRing
    {
        /// <summary>
        /// 电圈中心位置
        /// </summary>
        private readonly Vector2 _centerPosition;

        /// <summary>
        /// 最大范围（格子数）
        /// </summary>
        private readonly int _maxRange;

        /// <summary>
        /// 基础伤害
        /// </summary>
        private readonly int _damage;

        /// <summary>
        /// 麻痹持续时间（秒）
        /// </summary>
        private readonly float _paralyzeDuration;

        /// <summary>
        /// 游戏位置
        /// </summary>
        private readonly GameLocation _location;

        /// <summary>
        /// 已命中的敌人集合（防止重复伤害）
        /// </summary>
        private readonly HashSet<Monster> _hitMonsters = new();

        /// <summary>
        /// 当前半径（像素）
        /// </summary>
        private float _currentRadius;

        /// <summary>
        /// 扩散阶段计时器
        /// </summary>
        private float _expandTimer;

        /// <summary>
        /// 维持阶段计时器
        /// </summary>
        private float _sustainTimer;

        /// <summary>
        /// 淡出阶段计时器
        /// </summary>
        private float _fadeTimer;

        /// <summary>
        /// 电圈状态
        /// </summary>
        private RingState _state = RingState.Expanding;

        /// <summary>
        /// 伤害检测计时器（每隔一定时间检测一次范围内敌人）
        /// </summary>
        private float _damageCheckTimer;

        /// <summary>
        /// 动画计时器（用于多帧动画）
        /// </summary>
        private float _animationTimer;

        /// <summary>
        /// 伤害检测间隔
        /// </summary>
        private const float DamageCheckInterval = 0.05f;

        /// <summary>
        /// 扩散持续时间
        /// </summary>
        private const float ExpandDuration = 0.25f;

        /// <summary>
        /// 维持持续时间
        /// </summary>
        private const float SustainDuration = 0.1f;

        /// <summary>
        /// 淡出持续时间
        /// </summary>
        private const float FadeDuration = 0.15f;

        /// <summary>
        /// 精灵表总帧数
        /// </summary>
        private const int TotalFrames = 10;

        /// <summary>
        /// 每帧持续时间（秒）
        /// </summary>
        private const float FrameDuration = 0.05f;

        /// <summary>
        /// 当前帧索引
        /// </summary>
        private int _currentFrame = 0;

        /// <summary>
        /// 帧计时器
        /// </summary>
        private float _frameTimer = 0f;

        /// <summary>
        /// 电圈是否已完成
        /// </summary>
        public bool IsFinished { get; private set; }

        /// <summary>
        /// 创建电圈特效
        /// </summary>
        /// <param name="center">中心位置（世界坐标）</param>
        /// <param name="maxRange">最大范围（格子数，3或5）</param>
        /// <param name="damage">伤害值</param>
        /// <param name="paralyzeDuration">麻痹持续时间</param>
        /// <param name="location">游戏位置</param>
        public ElectricRing(Vector2 center, int maxRange, int damage, float paralyzeDuration, GameLocation location)
        {
            _centerPosition = center;
            _maxRange = maxRange;
            _damage = damage;
            _paralyzeDuration = paralyzeDuration;
            _location = location;
            _currentRadius = 0f;
        }

        /// <summary>
        /// 每帧更新电圈状态
        /// </summary>
        /// <param name="deltaTime">距离上一帧的时间（秒）</param>
        public void Update(float deltaTime)
        {
            if (IsFinished) return;

            // 更新帧动画
            _frameTimer += deltaTime;
            if (_frameTimer >= FrameDuration)
            {
                _frameTimer = 0f;
                _currentFrame = (_currentFrame + 1) % TotalFrames;
            }

            // 更新伤害检测计时器
            _damageCheckTimer += deltaTime;
            if (_damageCheckTimer >= DamageCheckInterval)
            {
                _damageCheckTimer = 0f;
                CheckDamage();
            }

            // 更新动画计时器
            _animationTimer += deltaTime;

            // 根据当前状态更新
            switch (_state)
            {
                case RingState.Expanding:
                    UpdateExpanding(deltaTime);
                    break;
                case RingState.Sustaining:
                    UpdateSustaining(deltaTime);
                    break;
                case RingState.Fading:
                    UpdateFading(deltaTime);
                    break;
            }
        }

        /// <summary>
        /// 更新扩散阶段
        /// </summary>
        private void UpdateExpanding(float deltaTime)
        {
            _expandTimer += deltaTime;
            float progress = _expandTimer / ExpandDuration;

            // 电圈从0逐渐扩大到最大范围
            // 每个格子是64像素
            float maxRadius = _maxRange * 64f / 2f;
            _currentRadius = maxRadius * Math.Min(progress, 1f);

            if (_expandTimer >= ExpandDuration)
            {
                _state = RingState.Sustaining;
                _sustainTimer = 0f;
            }
        }

        /// <summary>
        /// 更新维持阶段
        /// </summary>
        private void UpdateSustaining(float deltaTime)
        {
            _sustainTimer += deltaTime;

            if (_sustainTimer >= SustainDuration)
            {
                _state = RingState.Fading;
                _fadeTimer = 0f;
            }
        }

        /// <summary>
        /// 更新淡出阶段
        /// </summary>
        private void UpdateFading(float deltaTime)
        {
            _fadeTimer += deltaTime;

            if (_fadeTimer >= FadeDuration)
            {
                IsFinished = true;
            }
        }

        /// <summary>
        /// 检测并伤害范围内的敌人
        /// </summary>
        private void CheckDamage()
        {
            if (_location == null) return;

            // 获取范围内的所有怪物
            var monstersInRange = _location.characters
                .OfType<Monster>()
                .Where(m => !_hitMonsters.Contains(m) && IsInRange(m.Position))
                .ToList();

            foreach (var monster in monstersInRange)
            {
                // 造成伤害
                _location.damageMonster(
                    areaOfEffect: monster.GetBoundingBox(),
                    minDamage: _damage,
                    maxDamage: _damage,
                    isBomb: false,
                    who: Game1.player
                );

                // 应用麻痹效果
                ApplyParalyze(monster);

                // 标记已命中
                _hitMonsters.Add(monster);
            }
        }

        /// <summary>
        /// 判断怪物是否在电圈范围内
        /// </summary>
        private bool IsInRange(Vector2 monsterPosition)
        {
            float distance = Vector2.Distance(_centerPosition, monsterPosition);
            return distance <= _currentRadius;
        }

        /// <summary>
        /// 对怪物应用麻痹效果
        /// </summary>
        /// <param name="monster">目标怪物</param>
        private void ApplyParalyze(Monster monster)
        {
            // 使用游戏内置的眩晕机制（毫秒）
            monster.stunTime.Value = (int)(_paralyzeDuration * 1000);
        }

        /// <summary>
        /// 绘制电圈
        /// </summary>
        /// <param name="b">SpriteBatch</param>
        /// <param name="texture">纹理</param>
        public void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch b, Microsoft.Xna.Framework.Graphics.Texture2D texture)
        {
            if (_location != Game1.currentLocation) return;

            // 计算不透明度
            float alpha = 1f;
            switch (_state)
            {
                case RingState.Expanding:
                    alpha = _expandTimer / ExpandDuration;
                    break;
                case RingState.Sustaining:
                    alpha = 1f;
                    // 添加轻微闪烁效果
                    alpha = 0.8f + (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalSeconds * 20) * 0.2f;
                    break;
                case RingState.Fading:
                    alpha = 1f - (_fadeTimer / FadeDuration);
                    break;
            }

            // 计算缩放
            // 纹理是64x64，我们需要它覆盖 _currentRadius * 2 的直径
            float scale = (_currentRadius * 2f) / 64f;

            // 绘制位置（减去视口偏移）
            Vector2 drawPosition = Game1.GlobalToLocal(Game1.viewport, _centerPosition);

            // 颜色 - 紫色（符合山竹主题）
            Color color = Color.Purple * alpha;

            // 使用当前帧索引从精灵表中截取
            const int frameWidth = 64;
            Rectangle sourceRect = new Rectangle(_currentFrame * frameWidth, 0, frameWidth, frameWidth);

            b.Draw(
                texture,
                drawPosition,
                sourceRect, // 使用sourceRect选择当前帧
                color,
                0f, // 旋转
                new Vector2(32, 32), // 原点在帧中心
                scale,
                Microsoft.Xna.Framework.Graphics.SpriteEffects.None,
                1f // 层级
            );
        }

        /// <summary>
        /// 电圈状态枚举
        /// </summary>
        private enum RingState
        {
            /// <summary>扩散中</summary>
            Expanding,
            /// <summary>维持中</summary>
            Sustaining,
            /// <summary>淡出中</summary>
            Fading
        }
    }
}
