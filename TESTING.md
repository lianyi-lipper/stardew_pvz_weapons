# 聚能山竹饰品测试指南

## ⚡ 完整Trinket系统实现

**聚能山竹现在是真正的饰品 (Trinket)！**

- ✅ 数据驱动设计（Data/Trinkets.json）
- ✅ 装备在饰品栏
- ✅ 自动攻击附近敌人
- ✅ 按C键释放大招
- ✅ 无挥舞动画干扰

---

## 快速开始

### 1. 编译并安装

```powershell
cd e:\work\StardewPvZWeapons
dotnet build
```

编译成功后，文件会自动复制到Mods文件夹

### 2. 启动游戏

1. 通过SMAPI启动星露谷物语
2. 加载任意存档
3. 按 **`** (反引号键) 打开控制台

### 3. 获取饰品

在SMAPI控制台输入：

```
pvz_give_weapon primal_mangosteen
```

或简写/中文：
```
pvz_give_weapon mangosteen
pvz_give_weapon 山竹
```

### 4. 装备饰品

1. 打开背包（E键）
2. 找到"聚能山竹"
3. 将它拖到**饰品栏**（Trinket Slot）
4. 关闭背包

---

## 测试功能

### 测试自动攻击

1. 装备聚能山竹饰品
2. 进入矿洞
3. **靠近敌人**（3格范围内）
4. 观察效果：
   - ⚡ 自动生成3×3电圈（无需按键！）
   - 💥 敌人受到70伤害
   - 😵 敌人被麻痹1秒（速度变0）
   - ⏱️ 2秒后可再次触发

**注意**: 
- 自动检测附近敌人并释放电圈
- 类似"被动技能"
- 不需要任何按键操作普通攻击

### 测试大招

1. 等待冷却完成（装备后就绪）
2. **按C键**释放大招
3. 观察效果：
   - ⚡⚡ 玩家位置生成5×5巨型电圈
   - 💀 范围内敌人受到200伤害
   - ⏱️ 大招进入60秒CD
   - 💬 显示"⚡ 召唤闪电！"提示

**CD期间**：
- 按C键会提示剩余冷却时间
- 无法连续释放

---

## 饰品属性

| 属性 | 普通攻击 | 大招 |
|------|---------|------|
| 伤害 | 70 | 200 |
| 范围 | 3×3格 | 5×5格 |
| 触发方式 | 靠近敌人自动 | 按C键 |
| 冷却 | 2秒 | 60秒 |
| 麻痹时长 | 1秒 | 1秒 |
| 触发范围 | 192像素(3格) | - |

---

## 控制台指令

### pvz_give_weapon

```
pvz_give_weapon <name>
```

**可用名称**:
- `primal_mangosteen` - 完整名称
- `mangosteen` - 简写
- `山竹` - 中文

**内部ID**: `(TR)lianyi-lipper.StardewPvZWeapons_PrimalMangosteen`

---

## 技术细节

### Trinket系统架构

本mod正确实现了星露谷1.6的数据驱动Trinket系统：

1. **数据注册** - `Assets/Data/Trinkets.json`
   ```json
   {
     "lianyi-lipper.StardewPvZWeapons_PrimalMangosteen": {
       "DisplayName": "聚能山竹",
       "TrinketEffectClass": "StardewPvZWeapons.Domain.Effects.PrimalMangosteenEffect, StardewPvZWeapons"
     }
   }
   ```

2. **效果类** - `PrimalMangosteenEffect : TrinketEffect`
   - 继承自`TrinketEffect`
   - 实现自动攻击和大招逻辑
   - 由游戏系统自动实例化

3. **数据加载** - `DataLoader`
   - 通过`AssetRequested`事件注入
   - 将数据添加到`Data/Trinkets`

4. **物品创建** - `ItemRegistry.Create`
   - 使用`(TR)`前缀指定Trinket类型
   - 游戏自动关联TrinketEffect类

---

## 已知限制

**当前版本 v0.1.0-alpha**:

- ❌ 无电圈视觉特效（伤害生效，但看不到动画）
- ❌ 无闪电视觉特效
- ❌ 无击杀音效（有打击音效）
- ❌ 无制作配方（仅控制台）

**工作正常**:
- ✅ 自动检测敌人
- ✅ 造成伤害
- ✅ 麻痹效果（敌人速度变0）
- ✅ 大招CD系统
- ✅ 真实的Trinket（而非Ring冒充）

---

## 故障排除

### 问题：饰品无法获得

**可能原因**：
- mod未正确加载
- 数据注入失败

**检查SMAPI日志**：
```
[Stardew PvZ Weapons] 数据加载器已初始化
[Stardew PvZ Weapons] 已注册饰品: lianyi-lipper.StardewPvZWeapons_PrimalMangosteen
```

### 问题：饰品不攻击

**检查**:
- 饰品是否装备在**饰品栏**（不是背包）
- 是否靠近敌人（3格范围）
- 是否在冷却中（2秒CD）
- SMAPI控制台是否有错误

### 问题：C键无反应

**检查**:
- 饰品是否装备
- 是否在60秒CD中
- 查看游戏内提示信息

### 问题：看不到电圈

**说明**: 当前版本未实现视觉渲染，但**伤害和麻痹效果正常生效**。
- 敌人血量会减少
- 敌人会被定身（速度变0）
- 可以听到`thunder_small`音效

---

## 与戒指版本的区别

| 特性 | 戒指(Ring)版本 | 饰品(Trinket)版本 ✅ |
|-----|---------------|---------------------|
| 实现方式 | 继承Ring | 数据驱动+TrinketEffect |
| ID来源 | 借用Iridium Band | 独立ID注册 |
| 装备位置 | 戒指栏 | 饰品栏 |
| 冲突风险 | 可能与Iridium Band冲突 | 无冲突 |
| 真实性 | 冒充戒指 | 真实的饰品 |
| 符合度 | 低 | 高 |

---

## 下一步开发

1. **视觉特效** - 绘制电圈动画精灵图
2. **闪电特效** - 大招视觉效果
3. **音效** - 击杀电击音效
4. **制作配方** - 战斗8级解锁
5. **平衡性** - 根据测试调整数值

---

## 调试日志

SMAPI控制台关键日志：

```
[Stardew PvZ Weapons] 数据加载器已初始化
[Stardew PvZ Weapons] 已注册饰品: lianyi-lipper.StardewPvZWeapons_PrimalMangosteen
[Stardew PvZ Weapons] 武器注册系统已启动
[Stardew PvZ Weapons] 已将「聚能山竹」添加到背包
[Stardew PvZ Weapons] Ultimate ability triggered!
```
