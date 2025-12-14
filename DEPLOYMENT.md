# RimTalk-ExpandActions 部署说明

## ?? 部署状态：? 已完成

### 部署位置
```
D:\steam\steamapps\common\RimWorld\Mods\RimTalk-ExpandActions
```

### 已部署的文件清单

#### ?? 核心目录结构
```
RimTalk-ExpandActions/
├── About/
│   ├── About.xml          (1.28 KB) - Mod 元数据
│   ├── ModIcon.png        (1.36 KB) - Mod 图标
│   └── Preview.png        (330 KB)  - 预览图
│
├── Assemblies/
│   └── RimTalkExpandActions.dll (66 KB) - 主程序集（通用版本）
│
├── 1.5/
│   └── Assemblies/
│       └── RimTalkExpandActions.dll (66 KB) - RimWorld 1.5 专用版本
│
├── Defs/
│   ├── JobDefs/
│   │   └── Jobs_SocialDining.xml (0.8 KB)
│   ├── InteractionDefs/
│   │   └── Interaction_OfferFood.xml (1.35 KB)
│   └── ThoughtDefs/
│       └── Thoughts_SocialDining.xml (0.47 KB)
│
└── LoadFolders.xml (0.21 KB) - 版本加载配置
```

---

## ?? 快速部署命令

以后每次修改代码后，只需运行：

```powershell
.\Deploy.ps1
```

或者如果 RimWorld 安装在其他位置：

```powershell
.\Deploy.ps1 -RimWorldPath "C:\Program Files\RimWorld"
```

---

## ?? 自动部署配置

项目已配置 **MSBuild 自动部署**：

- ? 每次编译（Build）后自动复制 DLL 到项目的 `Assemblies` 和 `1.5/Assemblies` 目录
- ?? XML 定义文件需要手动运行 `Deploy.ps1` 部署到 RimWorld

---

## ?? 启动 RimWorld 前的检查清单

### 1. 确认文件完整性
所有关键文件已部署：
- ? `About\About.xml` (1306 bytes)
- ? `LoadFolders.xml` (217 bytes)
- ? `Assemblies\RimTalkExpandActions.dll` (67584 bytes)
- ? `1.5\Assemblies\RimTalkExpandActions.dll` (67584 bytes)
- ? `Defs\JobDefs\Jobs_SocialDining.xml` (823 bytes)
- ? `Defs\InteractionDefs\Interaction_OfferFood.xml` (1378 bytes)
- ? `Defs\ThoughtDefs\Thoughts_SocialDining.xml` (486 bytes)

### 2. Mod 依赖关系
确保已安装前置 Mod：
- ? **RimTalk-ExpandMemory** (必需)

### 3. 加载顺序
在 RimWorld Mod 管理器中确保：
```
Ludeon.RimWorld (核心)
    ↓
Ludeon.RimWorld.Royalty (如果有)
    ↓
RimTalk-ExpandMemory
    ↓
RimTalk-ExpandActions  ← 你的 Mod
```

---

## ?? 游戏内测试步骤

### 测试社交用餐功能

1. **启动游戏**
   - 在 Mod 列表中找到并启用 `RimTalk-ExpandActions`
   - 重启游戏

2. **创建/加载存档**
   - 确保有至少 2 个殖民者
   - 准备一些食物

3. **手动触发测试**（通过代码或开发者模式）
   ```csharp
   // 在开发者控制台执行
   using RimTalkExpandActions.SocialDining;
   
   Pawn pawn1 = Find.CurrentMap.mapPawns.FreeColonists[0];
   Pawn pawn2 = Find.CurrentMap.mapPawns.FreeColonists[1];
   
   if (FoodSharingUtility.TryFindSharedFood(pawn1, pawn2, out Thing food))
   {
       Job job = JobMaker.MakeJob(SocialDiningDefOf.SocialDine, food, null, pawn2);
       pawn1.jobs.TryTakeOrderedJob(job, JobTag.Misc);
   }
   ```

4. **观察效果**
   - 殖民者应该会前往食物位置
   - 拾取食物并找座位
   - 与伙伴一起进餐
   - 获得 "与人共餐" 心情加成 (+3)

---

## ?? 故障排查

### 问题 1：Mod 未出现在列表中
**解决方案：**
- 检查 `About\About.xml` 是否存在且编码正确（UTF-8）
- 确认目录名称为 `RimTalk-ExpandActions`

### 问题 2：游戏启动时报错
**解决方案：**
- 检查 `LoadFolders.xml` 是否存在
- 确认 DLL 文件已部署到 `Assemblies` 和 `1.5/Assemblies`
- 查看 RimWorld 日志文件：`%LOCALAPPDATA%Low\Ludeon Studios\RimWorld by Ludeon Studios\Player.log`

### 问题 3：XML 定义未加载
**解决方案：**
- 确认 `Defs` 文件夹及其子文件夹都已复制
- 检查 XML 文件编码为 UTF-8
- 重新运行 `.\Deploy.ps1`

### 问题 4：社交用餐功能不工作
**解决方案：**
- 确认命名空间更新正确：`RimTalkExpandActions.SocialDining`
- 检查游戏日志中的错误信息
- 确保至少有 2 个殖民者和可用食物

---

## ?? 开发工作流

### 日常开发流程
```bash
1. 修改代码（C#）
   ↓
2. 按 F6 编译（自动部署 DLL 到项目目录）
   ↓
3. 运行 Deploy.ps1（部署到 RimWorld）
   ↓
4. 启动 RimWorld 测试
```

### 仅修改 XML 定义
```bash
1. 修改 Defs/*.xml
   ↓
2. 运行 Deploy.ps1
   ↓
3. 重启 RimWorld（或重新加载 Defs）
```

---

## ?? 相关文件位置

- **项目源代码：** `C:\Users\Administrator\Desktop\rim mod\RimTalk-ExpandActions\`
- **RimWorld Mod：** `D:\steam\steamapps\common\RimWorld\Mods\RimTalk-ExpandActions\`
- **游戏日志：** `%LOCALAPPDATA%Low\Ludeon Studios\RimWorld by Ludeon Studios\Player.log`
- **部署脚本：** `.\Deploy.ps1`

---

## ? 当前状态

- ? 所有源代码文件已创建
- ? 命名空间已迁移到 `RimTalkExpandActions.SocialDining`
- ? XML 定义编码问题已修复
- ? 自动部署配置已添加到 `.csproj`
- ? 完整的 Mod 文件已部署到 RimWorld
- ? 项目编译成功

**?? Mod 已准备就绪，可以在游戏中启用和测试！**
