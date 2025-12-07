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
    using StardewPvZWeapons.Integration;
    using Microsoft.Xna.Framework.Graphics;
    using HarmonyLib;

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

            // 初始化 Harmony 补丁
            var harmony = new Harmony(this.ModManifest.UniqueID);
            SlingshotTexturePatch.Apply(harmony);
            CraftingRecipePatch.Apply(harmony);  // 配方创建时注入 modData
            Monitor.Log("Harmony patches applied", LogLevel.Trace);

            // 立即注册AssetRequested事件（必须在资产加载前）
            helper.Events.Content.AssetRequested += OnAssetRequested;

            // 注册其他事件监听器
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            helper.Events.Display.RenderedWorld += OnRenderedWorld;
            helper.Events.Player.InventoryChanged += OnInventoryChanged; // 蓝图转换为武器
            helper.Events.Content.LocaleChanged += OnLocaleChanged; // 语言切换时重新加载资产

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
                        Monitor.Log($"Registered {customTrinkets.Count} trinkets", LogLevel.Trace);
                    }
                    catch (Exception ex)
                    {
                        Monitor.Log($"加载Trinkets.json失败: {ex.Message}", LogLevel.Error);
                    }
                }, AssetEditPriority.Default);
            }
            
            // 2. 提供Trinket纹理
            if (e.NameWithoutLocale.IsEquivalentTo("Mods/lianyi-lipper.StardewPvZWeapons/Trinkets"))
            {
                e.LoadFromModFile<Texture2D>("Assets/Trinkets/Trinkets.png", AssetLoadPriority.Medium);
            }

            // 3. 提供电能豌豆武器贴图
            if (e.NameWithoutLocale.IsEquivalentTo("Mods/lianyi-lipper.StardewPvZWeapons/Weapons"))
            {
                e.LoadFromModFile<Texture2D>("Assets/Sprites/electric_gatling_pea.png", AssetLoadPriority.Medium);
            }
            // 4. 注入弹弓武器数据
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Weapons"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, StardewValley.GameData.Weapons.WeaponData>().Data;
                    
                    // 添加电能超级机枪射手（使用自定义贴图）
                    data["lianyi-lipper.StardewPvZWeapons_ElectricGatlingPea"] = new StardewValley.GameData.Weapons.WeaponData
                    {
                        Name = "ElectricGatlingPea",
                        DisplayName = Helper.Translation.Get("weapon.electric-gatling-pea.name"),
                        Description = Helper.Translation.Get("weapon.electric-gatling-pea.description"),
                        Type = 4, // 4 = Slingshot
                        Texture = "Mods/lianyi-lipper.StardewPvZWeapons/Weapons",
                        SpriteIndex = 0,
                        MinDamage = 50,
                        MaxDamage = 50,
                        CanBeLostOnDeath = false
                    };
                    
                    Monitor.Log("Registered slingshot weapon data", LogLevel.Trace);
                });
            }

            // 5. 注入蓝图物品到 Data/Objects（用于配方系统）
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, StardewValley.GameData.Objects.ObjectData>().Data;
                    
                    // 添加蓝图物品（获得后会自动转换为真正的弹弓）
                    data["lianyi-lipper.StardewPvZWeapons_ElectricGatlingPea_Blueprint"] = new StardewValley.GameData.Objects.ObjectData
                    {
                        Name = "ElectricGatlingPeaBlueprint",
                        DisplayName = Helper.Translation.Get("weapon.electric-gatling-pea.blueprint.name"),
                        Description = Helper.Translation.Get("weapon.electric-gatling-pea.blueprint.description"),
                        Type = "Crafting",
                        Category = -8, // Crafting
                        Price = 0,
                        Texture = "Mods/lianyi-lipper.StardewPvZWeapons/Weapons",
                        SpriteIndex = 0
                    };
                    
                    Monitor.Log("Registered blueprint object", LogLevel.Trace);
                });
            }

            // 6. 注入合成配方（产出蓝图物品）
            if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, string>().Data;
                    
                    // 配方: 电池组(787) x5 + 铱锭(337) x10 -> 蓝图物品
                    // 蓝图物品会在 InventoryChanged 事件中被替换为真正的弹弓
                    data["电能超级机枪射手"] = "787 5 337 10/Home/(O)lianyi-lipper.StardewPvZWeapons_ElectricGatlingPea_Blueprint 1/false/default";
                    
                    Monitor.Log("Registered crafting recipe", LogLevel.Trace);
                });
            }
        }

        /// <summary>
        /// 游戏启动完成事件
        /// </summary>
        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // 加载武器配置
            _configLoader = new Data.WeaponConfigLoader(Helper, Monitor);
            _configLoader.Load();
            Monitor.Log("Weapon config loaded", LogLevel.Trace);

            // 初始化武器注册系统
            _weaponRegistry = new Integration.WeaponRegistry(Helper, Monitor);
            _weaponRegistry.Initialize();

            // 初始化电能超级机枪射手管理器并传入配置
            _electricGatlingManager = new Services.ElectricGatlingPeaManager();
            _electricGatlingManager.Initialize(_configLoader.GetElectricGatlingPeaConfig());
            
            // 加载自定义子弹贴图
            try
            {
                var largeBulletTexture = Helper.ModContent.Load<Texture2D>("Assets/Sprites/bullet_large.png");
                var smallBulletTexture = Helper.ModContent.Load<Texture2D>("Assets/Sprites/bullet_small.png");
                _electricGatlingManager.SetBulletTextures(largeBulletTexture, smallBulletTexture);
                Monitor.Log("Bullet textures loaded", LogLevel.Trace);
                
                // 加载电弧特效贴图
                var arcTextures = new Texture2D[]
                {
                    Helper.ModContent.Load<Texture2D>("Assets/Sprites/electric_arc_0.png"),
                    Helper.ModContent.Load<Texture2D>("Assets/Sprites/electric_arc_1.png"),
                    Helper.ModContent.Load<Texture2D>("Assets/Sprites/electric_arc_2.png")
                };
                Domain.Weapons.CustomTexturedElectricProjectile.SetArcTextures(arcTextures);
                Monitor.Log("Arc effect textures loaded", LogLevel.Trace);
                
                // 加载武器贴图（物品栏图标）
                var weaponTexture = Helper.ModContent.Load<Texture2D>("Assets/Sprites/electric_gatling_pea.png");
                SlingshotTexturePatch.SetCustomTexture(weaponTexture);
                Monitor.Log("Weapon texture loaded", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                Monitor.Log($"加载贴图失败: {ex.Message}", LogLevel.Warn);
            }
            
            Monitor.Log("ElectricGatlingPea manager initialized", LogLevel.Trace);

            // 加载特效纹理并初始化EffectManager
            try
            {
                var ringTexture = Helper.ModContent.Load<Texture2D>("Assets/Effects/electric_ring.png");
                EffectManager.Instance.Initialize(ringTexture);
                Monitor.Log("Effect manager initialized", LogLevel.Trace);
                
                // 加载闪电纹理
                LightningTexture = Helper.ModContent.Load<Texture2D>("Assets/Effects/lightning_strike.png");
                Monitor.Log("Lightning texture loaded", LogLevel.Trace);
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

            // 更新电能超级机枪射手（通过 modData 检测）
            if (_electricGatlingManager != null && 
                player.CurrentTool is StardewValley.Tool tool &&
                Services.ElectricGatlingPeaManager.IsElectricGatlingPea(tool))
            {
                _electricGatlingManager.Update(1f / 60f, tool);
            }

            // 保留对旧版ICustomWeapon的支持（用于其他武器）
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
                            // 初始化配置（如果尚未初始化）
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
            
            // 绘制手持的自定义武器
            var player = Game1.player;
            if (player != null && 
                player.CurrentTool is StardewValley.Tools.Slingshot slingshot &&
                Services.ElectricGatlingPeaManager.IsElectricGatlingPea(slingshot))
            {
                SlingshotTexturePatch.DrawHeldWeapon(e.SpriteBatch, player);
            }
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

            // 电能超级机枪射手攻击处理（modData检测）
            if (_electricGatlingManager != null &&
                player.CurrentTool is StardewValley.Tool tool &&
                Services.ElectricGatlingPeaManager.IsElectricGatlingPea(tool))
            {
                // 关键：检查是否有菜单打开、玩家是否可以移动
                if (Game1.activeClickableMenu != null || !player.CanMove || player.UsingTool)
                {
                    goto SkipWeaponHandling;
                }

                // 只有左键 = 攻击
                if (e.Button == SButton.MouseLeft)
                {
                    _electricGatlingManager.OnAttack(player, player.currentLocation, tool);
                    Helper.Input.Suppress(e.Button);
                    return;
                }
            }

            // 保留对旧版ICustomWeapon的支持
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
                    string statusKey = newMode ? "weapon.electric-gatling-pea.destructive.on" : "weapon.electric-gatling-pea.destructive.off";
                    string statusMessage = Helper.Translation.Get(statusKey);
                    Game1.addHUDMessage(new HUDMessage(statusMessage, 2));
                    Helper.Input.Suppress(e.Button);
                    Monitor.Log($"Destructive mode toggled: {newMode}", LogLevel.Trace);
                    return;
                }

                // 保留对旧版BasePvZSlingshot的支持
                if (player.CurrentTool is BasePvZSlingshot slingshot)
                {
                    slingshot.DestructiveMode = !slingshot.DestructiveMode;
                    string statusKey = slingshot.DestructiveMode ? "weapon.electric-gatling-pea.destructive.on" : "weapon.electric-gatling-pea.destructive.off";
                    string statusMessage = Helper.Translation.Get(statusKey);
                    Game1.addHUDMessage(new HUDMessage(statusMessage, 2));
                    Helper.Input.Suppress(e.Button);
                    Monitor.Log($"Destructive mode toggled: {slingshot.DestructiveMode}", LogLevel.Trace);
                    return;
                }
            }

            // 检查是否按下C键（聚能山竹大招）
            if (e.Button == SButton.C)
            {
                Monitor.Log("C key pressed", LogLevel.Trace);
                
                // 检查玩家是否装备了聚能山竹饰品
                if (player.trinketItems.Count == 0)
                {
                    Monitor.Log("No trinkets equipped", LogLevel.Trace);
                    return;
                }
                
                Monitor.Log($"Player has {player.trinketItems.Count} trinkets equipped", LogLevel.Trace);
                
                foreach (var trinket in player.trinketItems)
                {
                    if (trinket != null)
                    {
                        Monitor.Log($"Checking trinket: {trinket.Name}, ItemId: {trinket.ItemId}", LogLevel.Trace);
                        
                        if (trinket.ItemId == "lianyi-lipper.StardewPvZWeapons_PrimalMangosteen")
                        {
                            Monitor.Log("Found Primal Mangosteen, getting effect", LogLevel.Trace);
                            
                            // 获取饰品效果
                            var effect = trinket.GetEffect();
                            if (effect != null)
                            {
                                Monitor.Log($"Effect type: {effect.GetType().FullName}", LogLevel.Trace);
                                
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
                                    Monitor.Log($"Effect type mismatch: {effect.GetType().FullName}", LogLevel.Trace);
                                }
                            }
                            else
                            {
                                Monitor.Log("GetEffect()返回null！", LogLevel.Error);
                            }
                        }
                    }
                }
                
                Monitor.Log("Primal Mangosteen trinket not found", LogLevel.Trace);
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

        /// <summary>
        /// 玩家背包变化事件
        /// 检测蓝图物品并替换为真正的弹弓
        /// </summary>
        private void OnInventoryChanged(object? sender, StardewModdingAPI.Events.InventoryChangedEventArgs e)
        {
            if (!e.IsLocalPlayer) return;

            const string BLUEPRINT_ID = "lianyi-lipper.StardewPvZWeapons_ElectricGatlingPea_Blueprint";
            
            var player = Game1.player;
            if (player == null) return;

            // 检查是否有蓝图物品需要转换
            for (int i = 0; i < player.Items.Count; i++)
            {
                var item = player.Items[i];
                if (item != null && item.ItemId == BLUEPRINT_ID)
                {
                    // 创建真正的弹弓武器
                    var slingshot = new StardewValley.Tools.Slingshot("lianyi-lipper.StardewPvZWeapons_ElectricGatlingPea");
                    slingshot.modData["lianyi-lipper.StardewPvZWeapons/WeaponType"] = "ElectricGatlingPea";
                    slingshot.modData["lianyi-lipper.StardewPvZWeapons/WeaponName"] = "ElectricGatlingPea";
                    
                    // 替换蓝图物品
                    player.Items[i] = slingshot;
                    
                    Monitor.Log("Blueprint converted to Electric Gatling Pea weapon", LogLevel.Trace);
                    string successMessage = Helper.Translation.Get("weapon.electric-gatling-pea.craft.success");
                    Game1.addHUDMessage(new HUDMessage(successMessage, 2));
                }
            }
        }

        /// <summary>
        /// 语言切换事件
        /// 使武器和物品数据资产失效以重新加载翻译
        /// </summary>
        private void OnLocaleChanged(object? sender, StardewModdingAPI.Events.LocaleChangedEventArgs e)
        {
            Monitor.Log($"Locale changed from {e.OldLocale} to {e.NewLocale}, invalidating assets...", LogLevel.Debug);
            
            // 使资产失效，触发重新加载以获取正确的翻译
            Helper.GameContent.InvalidateCache("Data/Weapons");
            Helper.GameContent.InvalidateCache("Data/Objects");
        }
    }
}
