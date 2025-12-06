# Stardew PvZ Weapons

> 将《植物大战僵尸2》的经典植物武器化到星露谷物语

## 功能特性

### 聚能山竹 (Primal Mangosteen)

电系近战武器，基于PvZ2原版机制设计：

- **普通攻击**：生成3×3扩散电圈，造成70伤害并麻痹敌人1秒
- **大招**：召唤5×5闪电电圈，造成200伤害（右键触发，CD 60秒）
- **冷却机制**：普攻2秒CD，防止无限连发
- **附魔支持**：兼容星露谷原生附魔系统（十字军、猛击等）

### 电能超级机枪射手 (Electric Gatling Pea)

电系远程弹弓武器，灵感来自PvZ2的机枪射手：

- **连射攻击**：每次发射4枚穿透电能子弹，造成50伤害并眩晕敌人
- **破坏模式**：按V键切换，可破坏树木、岩石等障碍物
- **终极技能**：30%几率触发电能爆发，持续1.5秒发射210枚子弹
- **穿透特性**：子弹可穿透多个敌人和障碍物

## 快速开始

### 安装

1. 确保已安装 [SMAPI](https://smapi.io/) 3.14.0+
2. 下载最新版本
3. 解压到 `Stardew Valley/Mods/` 目录

### 获取武器

打开SMAPI控制台（`键），输入：

```
pvz_give_weapon primal_mangosteen
pvz_give_weapon electric_gatling_pea
```

## 技术架构

项目采用分层架构设计：

```
Src/
├── Domain/          # 武器和特效逻辑
│   ├── Weapons/     # 武器类（弹射物等）
│   └── Effects/     # 特效类（电圈、闪电等）
├── Services/        # 武器管理器
├── Integration/     # 游戏集成（Harmony补丁）
└── ModEntry.cs      # SMAPI入口
```

## 依赖项

- .NET 6.0
- SMAPI 3.14.0+
- Stardew Valley 1.6+

## 许可

本mod为学习和娱乐用途。武器设计灵感来源于《植物大战僵尸2》，版权归EA/PopCap所有。

## 致谢

- 星露谷物语 - ConcernedApe
- 植物大战僵尸2 - EA/PopCap
- SMAPI - Pathoschild
- 武器与子弹贴图 - 青轴
