/*
 * Stardew PvZ Weapons Mod
 * 模块: ModEntry
 * 用途: SMAPI Mod入口点
 */

namespace StardewPvZWeapons
{
    using System;
    using System.Collections.Generic;
    using StardewModdingAPI;
    using StardewModdingAPI.Events;
    using StardewValley;
    using StardewPvZWeapons.Services;
    using StardewPvZWeapons.Domain.Weapons;
    using Microsoft.Xna.Framework.Graphics;

    /// <summary>
    /// Mod入口类
    /// 负责初始化mod、注册事件监听器
    /// </summary>
    public class ModEntry : Mod
    {
        /// <summary>
        /// Mod实例（单例）
        /// </summary>
        public static ModEntry? Instance { get; private set; }

        /// <summary>
        /// 武器注册器
        /// </summary>
        private Integration.WeaponRegistry? _weaponRegistry;

        /// <summary>
        /// 闪电纹理（供大招使用）
        /// </summary>
        public static Texture2D? LightningTexture { get; private set; }

        /// <summary>
        /// 武器配置加载器
        /// </summary>
        private Data.WeaponConfigLoader? _configLoader;

        /// <summary>
        /// 电能超级机枪射手管理器
        /// </summary>
        private Services.ElectricGatlingPeaManager? _electricGatlingManager;

        /// <summary>
        /// Mod入口点
        /// SMAPI会调用此方法来初始化mod
        /// </summary>
        /// <param name="helper">SMAPI提供的Helper接口</param>
        public override void Entry(IModHelper helper)
        {
            Instance = this;

            // ✅ 立即注册AssetRequested事件（必须在资产加载前）
            helper.Events.Content.AssetRequested += OnAssetRequested;

            // 注册其他事件监听器
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            helper.Events.Display.RenderedWorld += OnRenderedWorld;

            Monitor.Log("Stardew PvZ Weapons mod loaded successfully!", LogLevel.Info);
        }

        /// <summary>
        /// 资产请求事件 - 注入Trinket数据和纹理
        /// </summary>
        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            // 1. 注入Trinket数据
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Trinkets"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, StardewValley.GameData.TrinketData>().Data;
                    try
                    {
                        var customTrinkets = Helper.ModContent.Load<Dictionary<string, StardewValley.GameData.TrinketData>>("Assets/Data/Trinkets.json");
                        foreach (var trinket in customTrinkets)
                        {
                            data[trinket.Key] = trinket.Value;
                        }
                        Monitor.Log($"✅ 已注册 {customTrinkets.Count} 个饰品", LogLevel.Info);
                    }
                    catch (Exception ex)
                    {
                        Monitor.Log($"❌ 加载Trinkets.json失败: {ex.Message}", LogLevel.Error);
                    }
                }, AssetEditPriority.Default);
            }
            
            // 2. 提供Trinket纹理
            if (e.NameWithoutLocale.IsEquivalentTo("Mods/lianyi-lipper.StardewPvZWeapons/Trinkets"))
            {
                e.LoadFromModFile<Texture2D>("Assets/Trinkets/Trinkets.png", AssetLoadPriority.Medium);
            }

            // ✅ 3. 注入弹弓武器数据
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Weapons"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, StardewValley.GameData.Weapons.WeaponData>().Data;
                    
                    // 添加电能超级机枪射手
                    data["lianyi-lipper.StardewPvZWeapons_ElectricGatlingPea"] = new StardewValley.GameData.Weapons.WeaponData
                    {
                        Name = "电能超级机枪射手",
                        DisplayName = "电能超级机枪射手",
                        Description = "发射4枚穿透电能子弹，30%概率触发大招散射。",
                        Type = 4, // 4 = Slingshot
                        Texture = "TileSheets\\weapons",
                        SpriteIndex = 34, // 使用弹弓的精灵
                        MinDamage = 50,
                        MaxDamage = 50,
                        CanBeLostOnDeath = false
                    };
                    
                    Monitor.Log("✅ 已注册弹弓武器", LogLevel.Info);
                });
            }
        }

        /// <summary>
        /// 游戏启动完成事件
        /// </summary>
        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // ✅ 加载武器配置
            _configLoader = new Data.WeaponConfigLoader(Helper, Monitor);
            _configLoader.Load();
            Monitor.Log("✅ 武器配置系统已初始化", LogLevel.Info);

            // 初始化武器注册系统
            _weaponRegistry = new Integration.WeaponRegistry(Helper, Monitor);
            _weaponRegistry.Initialize();

            // ✅ 初始化电能超级机枪射手管理器并传入配置
            _electricGatlingManager = new Services.ElectricGatlingPeaManager();
            _electricGatlingManager.Initialize(_configLoader.GetElectricGatlingPeaConfig());
            Monitor.Log("✅ 电能超级机枪射手管理器已初始化", LogLevel.Info);

            // 加载特效纹理并初始化EffectManager
            try
            {
                var ringTexture = Helper.ModContent.Load<Texture2D>("Assets/Effects/electric_ring.png");
                EffectManager.Instance.Initialize(ringTexture);
                Monitor.Log("特效管理器已初始化", LogLevel.Info);
                
                // 加载闪电纹理
                LightningTexture = Helper.ModContent.Load<Texture2D>("Assets/Effects/lightning_strike.png");
                Monitor.Log("闪电纹理已加载", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Monitor.Log($"加载特效纹理失败: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// 每帧更新事件
        /// </summary>
        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            // 更新特效管理器
            EffectManager.Instance.Update(1f / 60f);

            var player = Game1.player;
            if (player == null) return;

            // ✅ 更新电能超级机枪射手（通过 modData 检测）
            if (_electricGatlingManager != null && 
                player.CurrentTool is StardewValley.Tool tool &&
                Services.ElectricGatlingPeaManager.IsElectricGatlingPea(tool))
            {
                _electricGatlingManager.Update(1f / 60f, tool);
            }

            // ✅ 保留对旧版ICustomWeapon的支持（用于其他武器）
            if (player.CurrentTool is Domain.Weapons.ICustomWeapon customWeapon)
            {
                customWeapon.Update(1f / 60f);
            }

            // 手动更新玩家装备的聚能山竹饰品效果
            if (player.trinketItems.Count > 0)
            {
                foreach (var trinket in player.trinketItems)
                {
                    if (trinket != null && trinket.ItemId == "lianyi-lipper.StardewPvZWeapons_PrimalMangosteen")
                    {
                        var effect = trinket.GetEffect();
                        if (effect is Domain.Effects.PrimalMangosteenEffect primalEffect)
                        {
                            // ✅ 初始化配置（如果尚未初始化）
                            if (_configLoader != null)
                            {
                                var config = _configLoader.GetPrimalMangosteenConfig();
                                primalEffect.Initialize(config);
                            }

                            primalEffect.Update(player);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 世界渲染事件
        /// </summary>
        private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            EffectManager.Instance.Draw(e.SpriteBatch);
        }

        /// <summary>
        /// 按键按下事件
        /// 用于处理C键触发大招
        /// </summary>
        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            var player = Game1.player;
            if (player == null) return;

            // ✅ 电能超级机枪射手攻击处理（modData检测）
            if (_electricGatlingManager != null &&
                player.CurrentTool is StardewValley.Tool tool &&
                Services.ElectricGatlingPeaManager.IsElectricGatlingPea(tool))
            {
                // 关键：检查是否有菜单打开、玩家是否可以移动
                if (Game1.activeClickableMenu != null || !player.CanMove || player.UsingTool)
                {
                    goto SkipWeaponHandling;
                }

                // 左键/使用工具键 = 普通攻击
                if (e.Button.IsActionButton() || e.Button.IsUseToolButton())
                {
                    _electricGatlingManager.OnAttack(player, player.currentLocation, tool);
                    Helper.Input.Suppress(e.Button);
                    return;
                }

                // 右键 = 特殊攻击（大招）
                if (e.Button == SButton.MouseRight)
                {
                    if (_electricGatlingManager.OnSpecialAttack(player, player.currentLocation, tool))
                    {
                        Helper.Input.Suppress(e.Button);
                    }
                    return;
                }
            }

            // ✅ 保留对旧版ICustomWeapon的支持
            if (player.CurrentTool is Domain.Weapons.ICustomWeapon slingshotWeapon)
            {
                if (Game1.activeClickableMenu != null || !player.CanMove || player.UsingTool)
                {
                    goto SkipWeaponHandling;
                }

                if (e.Button.IsActionButton() || e.Button.IsUseToolButton())
                {
                    slingshotWeapon.OnAttack(player, player.currentLocation);
                    Helper.Input.Suppress(e.Button);
                    return;
                }

                if (e.Button == SButton.MouseRight)
                {
                    if (slingshotWeapon.OnSpecialAttack(player, player.currentLocation))
                    {
                        Helper.Input.Suppress(e.Button);
                    }
                    return;
                }
            }

            SkipWeaponHandling:

            // V键 = 切换破坏模式（电能超级机枪射手）
            if (e.Button == SButton.V)
            {
                if (_electricGatlingManager != null &&
                    player.CurrentTool is StardewValley.Tool vlTool &&
                    Services.ElectricGatlingPeaManager.IsElectricGatlingPea(vlTool))
                {
                    bool newMode = !_electricGatlingManager.GetDestructiveMode(vlTool);
                    _electricGatlingManager.SetDestructiveMode(vlTool, newMode);
                    string status = newMode ? "开启" : "关闭";
                    Game1.addHUDMessage(new HUDMessage($"💥 破坏模式：{status}", 2));
                    Helper.Input.Suppress(e.Button);
                    Monitor.Log($"破坏模式已切换为: {status}", LogLevel.Info);
                    return;
                }

                // 保留对旧版BasePvZSlingshot的支持
                if (player.CurrentTool is BasePvZSlingshot slingshot)
                {
                    slingshot.DestructiveMode = !slingshot.DestructiveMode;
                    string status = slingshot.DestructiveMode ? "开启" : "关闭";
                    Game1.addHUDMessage(new HUDMessage($"💥 破坏模式：{status}", 2));
                    Helper.Input.Suppress(e.Button);
                    Monitor.Log($"破坏模式已切换为: {status}", LogLevel.Info);
                    return;
                }
            }

            // 检查是否按下C键（聚能山竹大招）
            if (e.Button == SButton.C)
            {
                Monitor.Log("检测到C键按下", LogLevel.Debug);
                
                // 检查玩家是否装备了聚能山竹饰品
                if (player.trinketItems.Count == 0)
                {
                    Monitor.Log("trinketItems为空", LogLevel.Warn);
                    return;
                }
                
                Monitor.Log($"玩家装备了 {player.trinketItems.Count} 个饰品", LogLevel.Debug);
                
                foreach (var trinket in player.trinketItems)
                {
                    if (trinket != null)
                    {
                        Monitor.Log($"检查饰品: {trinket.Name}, ItemId: {trinket.ItemId}", LogLevel.Debug);
                        
                        if (trinket.ItemId == "lianyi-lipper.StardewPvZWeapons_PrimalMangosteen")
                        {
                            Monitor.Log("找到聚能山竹，获取效果", LogLevel.Info);
                            
                            // 获取饰品效果
                            var effect = trinket.GetEffect();
                            if (effect != null)
                            {
                                Monitor.Log($"效果类型: {effect.GetType().FullName}", LogLevel.Info);
                                
                                if (effect is Domain.Effects.PrimalMangosteenEffect primalEffect)
                                {
                                    // 触发大招
                                    bool success = primalEffect.PerformUltimate(player, player.currentLocation);
                                    
                                    if (success)
                                    {
                                        Monitor.Log("Ultimate ability triggered!", LogLevel.Debug);
                                        // 抑制默认C键行为
                                        Helper.Input.Suppress(e.Button);
                                    }
                                    
                                    return; // 只触发第一个找到的饰品
                                }
                                else
                                {
                                    Monitor.Log($"效果类型不匹配: {effect.GetType().FullName}", LogLevel.Warn);
                                }
                            }
                            else
                            {
                                Monitor.Log("GetEffect()返回null！", LogLevel.Error);
                            }
                        }
                    }
                }
                
                Monitor.Log("未找到聚能山竹饰品", LogLevel.Debug);
            }
        }

        /// <summary>
        /// 存档加载完成事件
        /// </summary>
        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            Monitor.Log("Save loaded, initializing weapon systems...", LogLevel.Debug);
            
            // 清理特效管理器
            EffectManager.Instance.Clear();
        }

        /// <summary>
        /// 返回标题画面事件
        /// 用于清理资源
        /// </summary>
        private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            Monitor.Log("Returned to title, cleaning up...", LogLevel.Debug);
            
            // 清理特效管理器
            EffectManager.Instance.Clear();
        }
    }
}
