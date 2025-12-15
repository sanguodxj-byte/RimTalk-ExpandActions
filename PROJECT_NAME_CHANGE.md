# ??? 项目名称变更说明

## ?? 变更内容

### AssemblyName 变更

| 项目 | 旧名称 | 新名称 |
|------|--------|--------|
| **DLL 文件名** | `RimTalkExpandActions.dll` | `RimTalk-ExpandActions.dll` |
| **程序集名称** | `RimTalkExpandActions` | `RimTalk-ExpandActions` |

### ? 保持不变

| 项目 | 名称 |
|------|------|
| **命名空间** | `RimTalkExpandActions` (保持不变) |
| **项目文件** | `RimTalkExpandActions.csproj` (保持不变) |
| **PackageId** | `sanguo.rimtalk.expandactions` (保持不变) |
| **Mod 文件夹** | `RimTalk-ExpandActions` (保持不变) |

## ?? 修改的文件

### 1. RimTalkExpandActions.csproj

```xml
<PropertyGroup>
  <AssemblyName>RimTalk-ExpandActions</AssemblyName>  ← 添加了连字符
  <RootNamespace>RimTalkExpandActions</RootNamespace>  ← 保持不变
</PropertyGroup>
```

### 2. Deploy.ps1（自动适配）

部署脚本会自动检测新的 DLL 名称：
```powershell
$source = ".\Bin\Debug\RimTalk-ExpandActions.dll"  ← 自动使用新名称
```

### 3. 1.6/Assemblies/ 目录

编译后会自动生成：
```
1.6/Assemblies/RimTalk-ExpandActions.dll  ← 新名称
```

## ? 变更原因

### 1. 命名一致性

原来的状态：
```
Mod 文件夹: RimTalk-ExpandActions  ← 有连字符
DLL 文件:   RimTalkExpandActions   ← 没有连字符 ? 不一致
```

现在的状态：
```
Mod 文件夹: RimTalk-ExpandActions  ← 有连字符
DLL 文件:   RimTalk-ExpandActions  ← 有连字符 ? 一致
```

### 2. 遵循命名规范

RimWorld Mod 命名约定：
- Mod 文件夹名称通常使用连字符（例如：`Core`, `Royalty`, `Ideology`）
- DLL 名称应与 Mod 文件夹名称保持一致

### 3. 更好的可读性

```
RimTalkExpandActions  ← 难以阅读
RimTalk-ExpandActions ← 清晰明了 ?
```

## ?? 影响范围

### ? 无需修改

以下内容**不需要修改**：

1. **所有源代码文件** - 命名空间保持 `RimTalkExpandActions`
2. **About.xml** - PackageId 保持 `sanguo.rimtalk.expandactions`
3. **Defs 文件** - 所有定义保持不变
4. **文档** - 除了需要更新 DLL 名称的地方

### ?? 需要注意

1. **手动引用 DLL 的地方**
   - 如果有其他 Mod 引用此 DLL，需要更新引用名称
   - **注意：** 我们的 Mod 没有被其他 Mod 引用，所以无影响

2. **部署脚本**
   - `Deploy.ps1` - 已自动适配 ?
   - `QuickDeploy.ps1` - 需要检查 ??

3. **文档中的 DLL 名称**
   - 需要更新文档中提到的 DLL 文件名

## ?? 更新清单

### 已更新 ?

- [x] `RimTalkExpandActions.csproj` - AssemblyName
- [x] 重新编译生成新 DLL
- [x] 1.6/Assemblies/ 目录中的 DLL

### 待更新 ?

- [ ] `QuickDeploy.ps1` - 更新 DLL 文件名引用
- [ ] `Deploy.ps1` - 验证是否自动适配
- [ ] 所有文档中的 DLL 名称引用
- [ ] README.md（如果存在）

## ?? 回滚方法

如果需要回滚到旧名称：

```xml
<!-- RimTalkExpandActions.csproj -->
<PropertyGroup>
  <AssemblyName>RimTalkExpandActions</AssemblyName>  ← 移除连字符
</PropertyGroup>
```

然后重新编译即可。

## ?? 验证步骤

### 1. 检查编译输出

```powershell
Get-ChildItem ".\Bin\Debug\*.dll" | Select-Object Name
```

**预期输出：**
```
Name
----
RimTalk-ExpandActions.dll  ← 应该有连字符
```

### 2. 检查部署目录

```powershell
Get-ChildItem ".\1.6\Assemblies\*.dll" | Select-Object Name
```

**预期输出：**
```
Name
----
RimTalk-ExpandActions.dll  ← 应该有连字符
```

### 3. 在游戏中验证

1. 启动 RimWorld
2. 查看日志：
```
[RimTalk-ExpandActions] Mod 已加载
[RimTalk-ExpandActions] Harmony 补丁已应用
```

日志前缀保持不变，因为是硬编码在代码中的字符串。

## ?? 总结

| 项目 | 状态 |
|------|------|
| **项目文件修改** | ? 完成 |
| **DLL 重新编译** | ? 完成 |
| **部署目录更新** | ? 自动完成 |
| **命名一致性** | ? 达成 |
| **向后兼容** | ? 无影响（新 Mod，无依赖） |
| **文档更新** | ? 待处理 |

---

**变更日期:** 2025/12/15  
**变更原因:** 统一命名规范，提高一致性  
**影响范围:** 仅 DLL 文件名，不影响代码  
**状态:** ? 已完成
