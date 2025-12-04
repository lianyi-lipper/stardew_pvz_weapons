# Stardew PvZ Weapons

> 将《植物大战僵尸2》的经典植物武器化到星露谷物语

## 功能特性

### ⚡ 聚能山竹 (Primal Mangosteen)

电系近战武器，基于PvZ2原版机制设计：

- **普通攻击**：生成3×3扩散电圈，造成70伤害并麻痹敌人1秒
- **大招**：召唤5×5闪电电圈，造成200伤害（右键触发，CD 60秒）
- **冷却机制**：普攻2秒CD，防止无限连发
- **附魔支持**：兼容星露谷原生附魔系统（十字军、猛击等）

## 快速开始

### 安装

1. 确保已安装 [SMAPI](https://smapi.io/) 3.14.0+
2. 下载最新版本
3. 解压到 `Stardew Valley/Mods/` 目录

### 获取武器

打开SMAPI控制台（`键），输入：

```
pvz_give_weapon primal_mangosteen
```

详细测试指南见 [TESTING.md](file:///e:/work/StardewPvZWeapons/TESTING.md)

## 开发状态

**当前版本**: v0.1.0-alpha

### ✅ 已实现

- [x] 聚能山竹核心逻辑
- [x] 电圈扩散特效（逻辑）
- [x] 麻痹效果
- [x] 右键大招
- [x] 冷却系统
- [x] 控制台指令
- [x] 中英文本地化

### ⏳ 待实现

- [ ] 电圈视觉渲染
- [ ] 闪电视觉特效
- [ ] 击杀音效
- [ ] 制作配方
- [ ] 更多PvZ武器

## 技术架构

项目采用分层架构设计：

```
Src/
├── Domain/          # 武器和特效逻辑
│   ├── Weapons/     # 武器类
│   └── Effects/     # 特效类
├── Services/        # 特效管理器
├── Integration/     # 游戏集成
└── ModEntry.cs      # SMAPI入口
```

详见 [walkthrough.md](file:///C:/Users/19663/.gemini/antigravity/brain/46fbd18f-53e7-4dc4-b009-fda1435dda92/walkthrough.md)

## 开发文档

- [实施计划](file:///C:/Users/19663/.gemini/antigravity/brain/46fbd18f-53e7-4dc4-b009-fda1435dda92/implementation_plan.md)
- [设计规格](file:///C:/Users/19663/.gemini/antigravity/brain/46fbd18f-53e7-4dc4-b009-fda1435dda92/primal_mangosteen_spec.md)
- [开发进度](file:///C:/Users/19663/.gemini/antigravity/brain/46fbd18f-53e7-4dc4-b009-fda1435dda92/task.md)

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
