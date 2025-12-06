/*
 * Stardew PvZ Weapons Mod
 * 模块: Integration
 * 用途: Harmony 补丁 - 自定义弹弓贴图渲染
 */

namespace StardewPvZWeapons.Integration
{
    using HarmonyLib;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using StardewValley;
    using StardewValley.Tools;
    using System;

    /// <summary>
    /// 弹弓贴图补丁
    /// 用于渲染自定义武器贴图（物品栏和手持）
    /// </summary>
    public static class SlingshotTexturePatch
    {
        private static Texture2D? _customTexture;

        /// <summary>
        /// 设置自定义武器贴图
        /// </summary>
        public static void SetCustomTexture(Texture2D texture)
        {
            _customTexture = texture;
        }

        /// <summary>
        /// 应用 Harmony 补丁
        /// </summary>
        public static void Apply(Harmony harmony)
        {
            // 补丁物品栏图标绘制
            harmony.Patch(
                original: AccessTools.Method(typeof(Slingshot), nameof(Slingshot.drawInMenu),
                    new Type[] { typeof(SpriteBatch), typeof(Vector2), typeof(float), typeof(float), typeof(float), typeof(StackDrawType), typeof(Color), typeof(bool) }),
                prefix: new HarmonyMethod(typeof(SlingshotTexturePatch), nameof(DrawInMenu_Prefix))
            );
        }

        /// <summary>
        /// 前缀补丁 - 替换物品栏绘制
        /// </summary>
        public static bool DrawInMenu_Prefix(
            Slingshot __instance,
            SpriteBatch spriteBatch,
            Vector2 location,
            float scaleSize,
            float transparency,
            float layerDepth,
            StackDrawType drawStackNumber,
            Color color,
            bool drawShadow)
        {
            if (!IsElectricGatlingPea(__instance) || _customTexture == null)
            {
                return true;
            }

            spriteBatch.Draw(
                _customTexture,
                location + new Vector2(32f, 32f) * scaleSize,
                null,
                color * transparency,
                0f,
                new Vector2(_customTexture.Width / 2f, _customTexture.Height / 2f),
                scaleSize * 4f * (16f / _customTexture.Width),
                SpriteEffects.None,
                layerDepth
            );

            return false;
        }

        /// <summary>
        /// 绘制手持武器（从 ModEntry 调用）
        /// 武器会根据鼠标位置旋转，发射口朝向鼠标
        /// </summary>
        public static void DrawHeldWeapon(SpriteBatch b, Farmer player)
        {
            if (_customTexture == null) return;

            // 获取玩家位置（屏幕坐标）
            Vector2 playerPosition = player.getLocalPosition(Game1.viewport);
            Vector2 playerCenter = playerPosition + new Vector2(32, 48); // 玩家中心点
            
            // 获取鼠标位置（屏幕坐标）
            Vector2 mousePosition = new Vector2(Game1.getMouseX(), Game1.getMouseY());
            
            // 计算玩家到鼠标的方向向量
            Vector2 direction = mousePosition - playerCenter;
            
            // 获取玩家朝向
            int facingDirection = player.FacingDirection;
            bool mouseOnLeft = mousePosition.X < playerCenter.X;
            bool mouseOnRight = !mouseOnLeft;
            
            // 翻转逻辑：
            // 玩家朝向决定贴图的默认状态（朝左=翻转，朝右=不翻转）
            // 当鼠标在朝向的反方向时，反转这个默认状态
            bool shouldFlip;
            switch (facingDirection)
            {
                case 1: // 朝右：默认不翻转
                    // 如果鼠标在左侧（和朝向相反），则翻转
                    shouldFlip = mouseOnLeft;
                    break;
                case 3: // 朝左：默认翻转
                    // 如果鼠标在右侧（和朝向相反），则取消翻转
                    shouldFlip = mouseOnLeft; // 鼠标在左侧保持翻转，在右侧不翻转
                    break;
                case 0: // 朝上
                case 2: // 朝下
                default:
                    // 上下朝向时，根据鼠标在左/右决定
                    shouldFlip = mouseOnLeft;
                    break;
            }
            
            // 计算旋转角度（弧度）
            // Atan2 返回的角度：右=0, 下=π/2, 左=±π, 上=-π/2
            float rotation = (float)Math.Atan2(direction.Y, direction.X);
            
            // 水平镜像翻转，翻转时额外旋转180度
            SpriteEffects spriteEffect;
            if (shouldFlip)
            {
                spriteEffect = SpriteEffects.FlipHorizontally;
                rotation += MathHelper.Pi; // 翻转时旋转180度
            }
            else
            {
                spriteEffect = SpriteEffects.None;
            }
            
            // 根据朝向调整偏移
            Vector2 offset;
            float layerOffset = 0.01f;

            switch (facingDirection)
            {
                case 0: // 上
                    offset = new Vector2(0, -72);
                    layerOffset = -0.01f; // 在玩家后面
                    break;
                case 1: // 右
                    offset = new Vector2(30, -72);
                    break;
                case 2: // 下
                    offset = new Vector2(0, -72);
                    break;
                case 3: // 左
                    offset = new Vector2(-30, -72);
                    break;
                default:
                    offset = Vector2.Zero;
                    break;
            }

            Vector2 drawPosition = playerCenter + offset;
            float layerDepth = (player.StandingPixel.Y / 10000f) + layerOffset;

            b.Draw(
                _customTexture,
                drawPosition,
                null,
                Color.White,
                rotation,
                new Vector2(_customTexture.Width / 2f, _customTexture.Height / 2f),
                3.5f, // 更大的缩放
                spriteEffect,
                layerDepth
            );
        }

        /// <summary>
        /// 检查是否是电能超级机枪射手
        /// </summary>
        private static bool IsElectricGatlingPea(Slingshot slingshot)
        {
            return slingshot.modData.ContainsKey("lianyi-lipper.StardewPvZWeapons/WeaponType") &&
                   slingshot.modData["lianyi-lipper.StardewPvZWeapons/WeaponType"] == "ElectricGatlingPea";
        }
    }
}
