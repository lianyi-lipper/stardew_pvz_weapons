/*
 * Stardew PvZ Weapons Mod
 * 模块: Services
 * 用途: 特效管理器，负责管理所有电圈特效的生命周期
 */

namespace StardewPvZWeapons.Services
{
    using System.Collections.Generic;
    using Microsoft.Xna.Framework;
    using StardewValley;
    using StardewPvZWeapons.Domain.Effects;

    /// <summary>
    /// 特效管理器（单例）
    /// 管理所有电圈特效的创建、更新和清理
    /// </summary>
    public class EffectManager
    {
        private static EffectManager? _instance;

        /// <summary>
        /// 单例实例
        /// </summary>
        public static EffectManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new EffectManager();
                }
                return _instance;
            }
        }

        /// <summary>
        /// 活跃的电圈特效列表
        /// </summary>
        private readonly List<ElectricRing> _activeRings = new();

        /// <summary>
        /// 私有构造函数（单例模式）
        /// </summary>
        private EffectManager()
        {
        }

        /// <summary>
        /// 创建电圈特效
        /// </summary>
        /// <param name="center">中心位置（世界坐标）</param>
        /// <param name="maxRange">最大范围（格子数）</param>
        /// <param name="damage">伤害值</param>
        /// <param name="paralyzeDuration">麻痹持续时间</param>
        /// <param name="location">游戏位置</param>
        public void CreateElectricRing(Vector2 center, int maxRange, int damage, float paralyzeDuration, GameLocation location)
        {
            var ring = new ElectricRing(center, maxRange, damage, paralyzeDuration, location);
            _activeRings.Add(ring);
        }

        /// <summary>
        /// 每帧更新所有特效
        /// </summary>
        /// <param name="deltaTime">距离上一帧的时间（秒）</param>
        public void Update(float deltaTime)
        {
            // 更新所有活跃的电圈
            foreach (var ring in _activeRings)
            {
                ring.Update(deltaTime);
            }

            // 移除已完成的电圈
            _activeRings.RemoveAll(ring => ring.IsFinished);
        }

        /// <summary>
        /// 电圈纹理
        /// </summary>
        private Microsoft.Xna.Framework.Graphics.Texture2D? _ringTexture;

        /// <summary>
        /// 初始化特效管理器资源
        /// </summary>
        /// <param name="ringTexture">电圈纹理</param>
        public void Initialize(Microsoft.Xna.Framework.Graphics.Texture2D ringTexture)
        {
            _ringTexture = ringTexture;
        }

        /// <summary>
        /// 绘制所有特效
        /// </summary>
        /// <param name="b">SpriteBatch</param>
        public void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch b)
        {
            if (_ringTexture == null) return;

            foreach (var ring in _activeRings)
            {
                ring.Draw(b, _ringTexture);
            }
        }

        /// <summary>
        /// 清理所有特效（用于场景切换等情况）
        /// </summary>
        public void Clear()
        {
            _activeRings.Clear();
        }
    }
}
