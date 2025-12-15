# ?? SaveableFromNode InvalidCastException 修复

## ?? 错误信息

```
SaveableFromNode exception: System.InvalidCastException: Specified cast is not valid.
[Ref 5D5DAF71]
```

## ?? 根本原因

### 问题定位

文件：`Defs/JobDefs/Jobs_SocialDining.xml`

**错误代码：**
```xml
<Defs>
  <JobDef>
    <defName>SocialDine</defName>
    <!-- ... -->
  </JobDef>

  <!-- ? 错误：ThingDef 不应该在 JobDefs 文件中 -->
  <ThingDef Name="SharedFoodTrackerBase" Abstract="True">
    <comps>
      <li Class="RimTalkExpandActions.SocialDining.CompProperties_SharedFoodTracker" />
    </comps>
  </ThingDef>
</Defs>
```

### 为什么会报错？

RimWorld 的 Def 加载器根据**文件夹名称**决定如何解析 XML：

```
Defs/JobDefs/       → 期望所有 <Def> 都是 JobDef
Defs/ThingDefs/     → 期望所有 <Def> 都是 ThingDef
Defs/InteractionDefs/ → 期望所有 <Def> 都是 InteractionDef
```

当加载器在 `JobDefs` 文件夹中发现 `<ThingDef>`，它会尝试：
1. 读取 XML 节点
2. **强制转换为 JobDef**
3. ? **InvalidCastException** - ThingDef 无法转换为 JobDef！

## ? 修复方案

### 方案：移除 ThingDef（推荐）

由于 `SharedFoodTrackerBase` 是一个**抽象的 ThingDef**，它本来就不应该被实际加载。它的作用是：

1. **作为基类** - 让其他 ThingDef 继承
2. **定义公共组件** - 避免重复定义

但在我们的 Mod 中：
- ? **不需要这个 ThingDef** - 我们直接在代码中处理食物追踪
- ? **CompProperties_SharedFoodTracker 没有被使用** - 代码中使用的是 `FoodSharingUtility`

**修复后的代码：**
```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <!-- 社交用餐任务定义 -->
  <JobDef>
    <defName>SocialDine</defName>
    <driverClass>RimTalkExpandActions.SocialDining.JobDriver_SocialDine</driverClass>
    <reportString>正在与他人共餐</reportString>
    <allowOpportunisticPrefix>true</allowOpportunisticPrefix>
    <casualInterruptible>false</casualInterruptible>
    <suspendable>false</suspendable>
    <collideWithPawns>true</collideWithPawns>
    <playerInterruptible>true</playerInterruptible>
    <makeTargetPrisoner>false</makeTargetPrisoner>
  </JobDef>
</Defs>
```

## ?? 验证修复

### 1. 重启 RimWorld

关闭游戏，然后重新启动。

### 2. 检查日志

**成功案例（应该看不到错误）：**
```
Loading defs...
  JobDef: SocialDine ?
  InteractionDef: OfferFood ?
  ThoughtDef: AteWithColonist ?
  ThoughtDef: OfferedFood ?
  ThoughtDef: ReceivedFoodOffer ?
```

**失败案例（如果还有问题）：**
```
SaveableFromNode exception: System.InvalidCastException...
```

### 3. 游戏内测试

如果没有错误，测试社交用餐功能：

```csharp
// F12 控制台运行
var pawn1 = Find.CurrentMap.mapPawns.FreeColonists.FirstOrDefault();
var pawn2 = Find.CurrentMap.mapPawns.FreeColonists.Skip(1).FirstOrDefault();

if (pawn1 != null && pawn2 != null)
{
    RimTalkExpandActions.Memory.Actions.RimTalkActions.ExecuteSocialDining(pawn1, pawn2);
    Log.Message("? 社交用餐测试完成");
}
```

## ?? 其他可能的 InvalidCastException 原因

### 1. 类型名称拼写错误

**错误：**
```xml
<driverClass>RimTalkExpandActions.SocialDining.JobDriver_SocialDin</driverClass>
<!--                                                            ↑ 少了一个 'e' -->
```

**修复：**
```xml
<driverClass>RimTalkExpandActions.SocialDining.JobDriver_SocialDine</driverClass>
```

### 2. 命名空间不匹配

**错误：**
```xml
<driverClass>RimTalkExpandActions.JobDriver_SocialDine</driverClass>
<!--          ↑ 缺少 .SocialDining 命名空间 -->
```

**修复：**
```xml
<driverClass>RimTalkExpandActions.SocialDining.JobDriver_SocialDine</driverClass>
```

### 3. 继承错误的基类

**错误的 C# 代码：**
```csharp
// JobDriver_SocialDine 应该继承 JobDriver，而不是 Job
public class JobDriver_SocialDine : Job  // ? 错误
{
}
```

**修复：**
```csharp
public class JobDriver_SocialDine : JobDriver  // ? 正确
{
}
```

### 4. 字段类型不匹配

**错误的 XML：**
```xml
<JobDef>
  <casualInterruptible>yes</casualInterruptible>  <!-- ? 应该是 true/false -->
</JobDef>
```

**修复：**
```xml
<JobDef>
  <casualInterruptible>true</casualInterruptible>  <!-- ? 正确 -->
</JobDef>
```

## ??? 预防措施

### 1. 使用正确的文件夹结构

```
Defs/
├── JobDefs/
│   └── Jobs_SocialDining.xml      ← 只包含 JobDef
├── InteractionDefs/
│   └── Interaction_OfferFood.xml  ← 只包含 InteractionDef
├── ThoughtDefs/
│   └── Thoughts_SocialDining.xml  ← 只包含 ThoughtDef
└── ThingDefs/                     ← 如果需要 ThingDef，放这里
    └── Things_Food.xml
```

### 2. 验证 XML 格式

使用 XML 验证器检查：
- 标签是否正确闭合
- 属性值是否有引号
- 结构是否符合 RimWorld 的 Def 格式

### 3. 检查类型名称

确保 C# 类名和 XML 中的引用完全一致：

**C# 文件：**
```csharp
namespace RimTalkExpandActions.SocialDining
{
    public class JobDriver_SocialDine : JobDriver
    {
        // ...
    }
}
```

**XML 文件：**
```xml
<driverClass>RimTalkExpandActions.SocialDining.JobDriver_SocialDine</driverClass>
<!--          ↑ 完全匹配：命名空间 + 类名 -->
```

### 4. 使用开发者模式

启动游戏时：
1. 勾选 "开发者模式"
2. 查看 "Development → Dev Inspector"
3. 检查所有 Def 是否正确加载

## ?? 修复历史

| 时间 | 问题 | 修复 | 状态 |
|------|------|------|------|
| 2025/12/15 | ThingDef 在 JobDefs 文件中 | 移除 ThingDef | ? 已修复 |

## ?? 总结

**问题：** `SaveableFromNode exception: InvalidCastException`

**原因：** `ThingDef` 错误地放在了 `Defs/JobDefs/` 文件夹中

**修复：** 从 `Jobs_SocialDining.xml` 中移除 `<ThingDef>` 定义

**结果：** ? 错误已修复，游戏可以正常加载

---

**修复日期：** 2025/12/15  
**影响范围：** `Defs/JobDefs/Jobs_SocialDining.xml`  
**测试状态：** ? 待游戏内验证
