# ?? ImportFromExternalMod 诊断指南

## 问题：未找到 ImportFromExternalMod 方法

### ?? 背景知识

`ImportFromExternalMod` 是 **RimTalk-ExpandMemory 的官方 API**，用于跨 Mod 集成。

#### API 设计

```csharp
// RimTalk-ExpandMemory 的 CommonKnowledgeLibrary 类
public class CommonKnowledgeLibrary
{
    /// <summary>
    /// 从外部 Mod 导入常识规则
    /// </summary>
    /// <param name="knowledgeText">规则文本（支持多行格式）</param>
    /// <param name="sourceModName">来源 Mod 名称</param>
    /// <param name="overwriteExisting">是否覆盖已有规则</param>
    /// <returns>成功导入的规则数量</returns>
    public int ImportFromExternalMod(string knowledgeText, string sourceModName, bool overwriteExisting)
    {
        // 解析规则文本
        // 添加到常识库
        // 返回导入数量
    }
}
```

#### 为什么使用这个方法？

| 原因 | 说明 |
|------|------|
| **官方 API** | ExpandMemory 专门提供的集成接口 |
| **存档隔离** | 每个存档有独立的常识库实例 |
| **来源追踪** | 记录规则来自哪个 Mod |
| **版本管理** | 支持规则更新和覆盖 |
| **格式标准** | 统一的规则格式和解析 |

---

## ?? 诊断步骤

### 1. 检查 ExpandMemory 版本

运行游戏后，查看日志：

```
[RimTalk-ExpandActions] CommonKnowledge 类型: RimTalk.Memory.CommonKnowledgeLibrary
[RimTalk-ExpandActions] 程序集: RimTalkMemoryPatch
[RimTalk-ExpandActions] CommonKnowledge 可用方法:
  - ImportFromExternalMod(String knowledgeText, String sourceModName, Boolean overwriteExisting)
  - AddKnowledgeEntry(KnowledgeEntry entry)
  - RemoveKnowledgeEntry(String id)
  ...
```

**如果看到 `ImportFromExternalMod`：** ? 版本兼容

**如果没有看到：** ? 版本太旧或不兼容

### 2. 确认 ExpandMemory 安装

检查 Mod 列表：

```
Mod 加载顺序：
1. Harmony
2. Core
3. RimTalk
4. RimTalk-ExpandMemory  ← 必须在这里
5. RimTalk-ExpandActions  ← 我们的 Mod
```

**PackageId 验证：**
- ? 正确：`sanguo.rimtalk.expandmemory`
- ? 错误：`RimTalk.ExpandMemory` 或其他

### 3. 查看完整日志

日志文件位置：
```
%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log
```

搜索关键词：
- `[RimTalk-ExpandActions]` - 我们的 Mod 日志
- `CommonKnowledge 可用方法` - 方法列表
- `未找到 ImportFromExternalMod` - 错误信息

---

## ? 已实现的诊断功能

### 自动检测方法签名

代码中已添加：

```csharp
// 1. 列出所有可用方法
Log.Message("[RimTalk-ExpandActions] CommonKnowledge 可用方法:");
foreach (var method in commonKnowledgeType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
{
    var pars = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
    Log.Message($"[RimTalk-ExpandActions]   - {method.Name}({pars})");
}

// 2. 尝试查找标准签名
MethodInfo importMethod = commonKnowledgeType.GetMethod(
    "ImportFromExternalMod",
    BindingFlags.Public | BindingFlags.Instance,
    null,
    new Type[] { typeof(string), typeof(string), typeof(bool) },
    null
);

// 3. 如果未找到，尝试任何同名方法
if (importMethod == null)
{
    var allImportMethods = commonKnowledgeType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Where(m => m.Name == "ImportFromExternalMod")
        .ToList();
    
    if (allImportMethods.Any())
    {
        importMethod = allImportMethods.First();
        Log.Message("[RimTalk-ExpandActions] 使用找到的方法（可能签名不同）");
    }
}
```

---

## ?? 常见问题

### Q1: 为什么不直接访问 CommonKnowledgeLibrary？

**A:** 因为 RimTalk-ExpandMemory 是**另一个 Mod**，我们不能直接引用它的类型：
- ? **使用反射** - 运行时动态查找（当前方案）
- ? **添加引用** - 编译时依赖（会导致加载顺序问题）

### Q2: 为什么不使用静态方法？

**A:** `ImportFromExternalMod` 是**实例方法**，因为：
- 每个存档有独立的 `CommonKnowledge` 实例
- 需要访问存档级别的数据
- 支持多存档并存

### Q3: 为什么会找不到方法？

**可能原因：**

| 原因 | 解决方案 |
|------|---------|
| ExpandMemory 版本太旧 | 更新到最新版本 |
| 方法签名变化 | 查看日志中的方法列表 |
| Mod 未正确加载 | 检查加载顺序 |
| PackageId 错误 | 使用 `sanguo.rimtalk.expandmemory` |

### Q4: 可以用其他方法吗？

**备选方案：**

1. **AddKnowledgeEntry** - 逐条添加规则
   ```csharp
   // 需要创建 KnowledgeEntry 对象
   var entry = new KnowledgeEntry { ... };
   commonKnowledge.AddKnowledgeEntry(entry);
   ```
   **缺点：** 需要了解 `KnowledgeEntry` 的内部结构

2. **直接修改文件** - 编辑常识库文件
   ```
   [存档目录]/CommonKnowledge.xml
   ```
   **缺点：** 无法动态更新，用户体验差

3. **等待 API** - 联系 ExpandMemory 作者
   **缺点：** 时间不确定

**结论：** `ImportFromExternalMod` 是**最佳选择**，因为它是官方 API，设计用于此场景。

---

## ?? 测试结果

### 成功案例

```
[RimTalk-ExpandActions] ━━━━━ 手动注入常识库 ━━━━━
[RimTalk-ExpandActions] ? 当前有活动的游戏存档
[RimTalk-ExpandActions] ? 找到 MemoryManager: RimTalk.Memory.MemoryManager
[RimTalk-ExpandActions] ? 找到当前存档的 MemoryManager 实例
[RimTalk-ExpandActions] ? 找到 CommonKnowledge 实例
[RimTalk-ExpandActions] CommonKnowledge 类型: RimTalk.Memory.CommonKnowledgeLibrary
[RimTalk-ExpandActions] 程序集: RimTalkMemoryPatch
[RimTalk-ExpandActions] CommonKnowledge 可用方法:
  - ImportFromExternalMod(String knowledgeText, String sourceModName, Boolean overwriteExisting)
  - AddKnowledgeEntry(KnowledgeEntry entry)
  - GetKnowledgeByTag(String tag)
  - ...
[RimTalk-ExpandActions] ? 找到 ImportFromExternalMod 方法
[RimTalk-ExpandActions] 准备注入 8 种行为规则...
[CommonKnowledge API] RimTalk-ExpandActions imported 8 knowledge entries
[RimTalk-ExpandActions] ━━━━━━━━━━━━━━━━━━━━━━━━━━
[RimTalk-ExpandActions] ??? 成功导入 8 条规则到当前存档 ???
[RimTalk-ExpandActions] ━━━━━━━━━━━━━━━━━━━━━━━━━━
```

### 失败案例

```
[RimTalk-ExpandActions] ━━━━━ 手动注入常识库 ━━━━━
[RimTalk-ExpandActions] ? 当前有活动的游戏存档
[RimTalk-ExpandActions] ? 找到 MemoryManager: RimTalk.Memory.MemoryManager
[RimTalk-ExpandActions] ? 找到当前存档的 MemoryManager 实例
[RimTalk-ExpandActions] ? 找到 CommonKnowledge 实例
[RimTalk-ExpandActions] CommonKnowledge 类型: RimTalk.Memory.CommonKnowledgeLibrary
[RimTalk-ExpandActions] 程序集: RimTalkMemoryPatch
[RimTalk-ExpandActions] CommonKnowledge 可用方法:
  - AddKnowledgeEntry(KnowledgeEntry entry)
  - GetKnowledgeByTag(String tag)
  - ...  ← 没有 ImportFromExternalMod！
[RimTalk-ExpandActions] 标准签名未找到，尝试其他签名...
[RimTalk-ExpandActions] ? 未找到 ImportFromExternalMod 方法（版本不兼容）
[RimTalk-ExpandActions] 请更新 RimTalk-ExpandMemory 到最新版本
```

**解决方案：** 更新 ExpandMemory 到包含此 API 的版本

---

## ?? 最佳实践

### 1. 检查版本兼容性

在 `About.xml` 中声明依赖：

```xml
<modDependencies>
    <li>
        <packageId>sanguo.rimtalk.expandmemory</packageId>
        <displayName>RimTalk-ExpandMemory</displayName>
        <steamWorkshopUrl>steam://url/CommunityFilePage/[ID]</steamWorkshopUrl>
    </li>
</modDependencies>
```

### 2. 提供降级方案

如果 API 不可用，提示用户：

```csharp
if (importMethod == null)
{
    Messages.Message(
        "请更新 RimTalk-ExpandMemory 到最新版本以使用自动注入功能",
        MessageTypeDefOf.RejectInput
    );
}
```

### 3. 记录详细日志

帮助用户和开发者诊断问题：

```csharp
Log.Message($"[Mod] 尝试调用: {methodName}");
Log.Message($"[Mod] 参数: {string.Join(", ", parameters)}");
Log.Message($"[Mod] 返回值: {result}");
```

---

## ?? 相关链接

- **RimTalk GitHub:** [RimTalk 主仓库]
- **ExpandMemory GitHub:** [ExpandMemory 仓库]
- **API 文档:** [CommonKnowledgeLibrary API]

---

## ?? 总结

| 问题 | 答案 |
|------|------|
| **为什么用 ImportFromExternalMod？** | 这是官方 API，专门用于跨 Mod 集成 |
| **找不到方法怎么办？** | 更新 ExpandMemory 到最新版本 |
| **有其他方案吗？** | 有，但都不如官方 API 好用 |
| **如何调试？** | 查看日志中的方法列表和错误信息 |

**结论：** `ImportFromExternalMod` 是正确的选择，如果找不到方法，说明 ExpandMemory 版本需要更新。

---

**更新日期：** 2025/12/15  
**Mod 版本：** v1.1.0  
**ExpandMemory 最低版本：** [待确认]
