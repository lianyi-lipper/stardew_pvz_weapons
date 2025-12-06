/*
 * Stardew PvZ Weapons Mod
 * 模块: Domain / Weapons
 * 用途: 带自定义贴图和电弧特效的电能穿透投射物类
 */

namespace StardewPvZWeapons.Domain.Weapons
{
    using StardewValley;
    using StardewValley.Monsters;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 电弧特效数据
    /// </summary>
    public class ElectricArc
    {
        public int TextureIndex;      // 使用哪个电弧贴图 (0-2)
        public Vector2 Offset;        // 相对于弹体中心的偏移
        public float Rotation;        // 旋转角度
        public float Lifetime;        // 生命周期
        public float MaxLifetime;     // 最大生命周期
        public float Alpha;           // 当前透明度
    }

    /// <summary>
    /// 带自定义贴图和电弧特效的电能穿透投射物
    /// </summary>
    public class CustomTexturedElectricProjectile : ElectricPiercingProjectile
    {
        private Texture2D _customTexture;
        private Rectangle _sourceRect;
        private float _scale;
        
        // 电弧特效相关
        private static Texture2D[]? _arcTextures;
        private List<ElectricArc> _arcs = new List<ElectricArc>();
        private Random _random = new Random();
        private const int MIN_ARCS = 2;
        private const int MAX_ARCS = 3;
        private const float ARC_MIN_LIFETIME = 0.1f;
        private const float ARC_MAX_LIFETIME = 0.2f;

        public CustomTexturedElectricProjectile(
            int damage,
            Vector2 startPosition,
            float xVelocity,
            float yVelocity,
            GameLocation location,
            Character firer,
            float maxDistance,
            Texture2D customTexture,
            float scale = 4f,
            float stunDuration = 1000f,
            bool destructiveMode = false
        ) : base(
            damage,
            10,
            startPosition,
            xVelocity,
            yVelocity,
            location,
            firer,
            maxDistance,
            stunDuration,
            destructiveMode
        )
        {
            _customTexture = customTexture;
            _sourceRect = new Rectangle(0, 0, 16, 16);
            _scale = scale;
            
            // 初始化电弧
            InitializeArcs();
        }

        /// <summary>
        /// 设置电弧贴图（静态，所有投射物共享）
        /// </summary>
        public static void SetArcTextures(Texture2D[] textures)
        {
            _arcTextures = textures;
        }

        /// <summary>
        /// 初始化电弧
        /// </summary>
        private void InitializeArcs()
        {
            if (_arcTextures == null || _arcTextures.Length == 0) return;

            int arcCount = _random.Next(MIN_ARCS, MAX_ARCS + 1);
            for (int i = 0; i < arcCount; i++)
            {
                _arcs.Add(CreateNewArc());
            }
        }

        /// <summary>
        /// 创建新电弧
        /// </summary>
        private ElectricArc CreateNewArc()
        {
            // 在弹体边缘随机位置生成电弧
            float angle = (float)(_random.NextDouble() * Math.PI * 2);
            float radius = 6f * _scale; // 弹体半径附近
            
            return new ElectricArc
            {
                TextureIndex = _random.Next(0, _arcTextures!.Length),
                Offset = new Vector2(
                    (float)Math.Cos(angle) * radius,
                    (float)Math.Sin(angle) * radius
                ),
                Rotation = angle + (float)(_random.NextDouble() - 0.5) * 1.5f, // 随机旋转
                MaxLifetime = ARC_MIN_LIFETIME + (float)_random.NextDouble() * (ARC_MAX_LIFETIME - ARC_MIN_LIFETIME),
                Lifetime = 0f,
                Alpha = 1f
            };
        }

        /// <summary>
        /// 更新电弧状态
        /// </summary>
        private void UpdateArcs(float deltaTime)
        {
            if (_arcTextures == null || _arcTextures.Length == 0) return;

            for (int i = _arcs.Count - 1; i >= 0; i--)
            {
                var arc = _arcs[i];
                arc.Lifetime += deltaTime;

                // 计算透明度衰减
                float progress = arc.Lifetime / arc.MaxLifetime;
                arc.Alpha = 1f - progress;

                // 电弧消失后，生成新电弧
                if (arc.Lifetime >= arc.MaxLifetime)
                {
                    _arcs[i] = CreateNewArc();
                }
            }

            // 保持电弧数量在范围内
            while (_arcs.Count < MIN_ARCS)
            {
                _arcs.Add(CreateNewArc());
            }
        }

        public override bool update(GameTime time, GameLocation location)
        {
            // 更新电弧
            float deltaTime = (float)time.ElapsedGameTime.TotalSeconds;
            UpdateArcs(deltaTime);

            return base.update(time, location);
        }

        public override void draw(SpriteBatch b)
        {
            if (_customTexture == null)
            {
                base.draw(b);
                return;
            }

            // 计算绘制位置
            Vector2 drawPos = Game1.GlobalToLocal(
                Game1.viewport, 
                this.position.Value
            );

            // 计算旋转角度
            float rotation = (float)Math.Atan2(this.yVelocity.Value, this.xVelocity.Value);
            float depth = (this.position.Value.Y + 32) / 10000f;

            // 1. 先绘制电弧（在弹体下方）
            if (_arcTextures != null && _arcTextures.Length > 0)
            {
                foreach (var arc in _arcs)
                {
                    if (arc.TextureIndex < _arcTextures.Length)
                    {
                        var arcTexture = _arcTextures[arc.TextureIndex];
                        Vector2 arcPos = drawPos + arc.Offset;
                        
                        b.Draw(
                            arcTexture,
                            arcPos,
                            null,
                            Color.White * arc.Alpha,
                            arc.Rotation,
                            new Vector2(arcTexture.Width / 2f, arcTexture.Height / 2f),
                            _scale * 0.8f, // 电弧稍微小一点
                            SpriteEffects.None,
                            depth - 0.0001f
                        );
                    }
                }
            }

            // 2. 绘制弹体主体
            b.Draw(
                _customTexture,
                drawPos,
                _sourceRect,
                Color.White,
                rotation,
                new Vector2(8, 8),
                _scale,
                SpriteEffects.None,
                depth
            );
        }
    }
}
