# 修复 Mod 设置界面错误

## 错误信息
```
Exception filling window for RimWorld.Dialog_ModSettings: 
System.MissingMethodException: Method not found: 
UnityEngine.Rect Verse.Listing_Standard.Label(string,single,string)
```

## 问题原因

RimWorld 1.6 中 `Listing_Standard.Label` 方法的签名已更改，不再支持多参数版本。

**错误代码（旧版本）：**
```csharp
listingStandard.Label("文本", -1f, "提示");  // ? 在 RimWorld 1.6 中不存在
```

**正确代码（已修复）：**
```csharp
listingStandard.Label("文本");  // ? RimWorld 1.6 正确用法
```

## 已修复

此问题已在以下文件中修复：
- `Source/RimTalkExpandActionsMod.cs` - `DoSettingsWindowContents` 方法

**修复内容：**
```csharp
public override void DoSettingsWindowContents(Rect inRect)
{
    Listing_Standard listingStandard = new Listing_Standard();
    listingStandard.Begin(inRect);
    
    // ? 所有 Label 调用都只使用一个 string 参数
    listingStandard.Label("RimTalk-ExpandActions v1.1.0");
    listingStandard.Gap();
    listingStandard.Label("7种行为系统已启用（招募/投降/恋爱/灵感/休息/赠送/用餐）");
    listingStandard.Label("所有行为默认100%成功率");
    listingStandard.Label("自动注入规则功能已启用");
    listingStandard.Gap();
    listingStandard.Label("详细设置请编辑配置文件或通过代码调用。");
    
    listingStandard.End();
}
```

## 如何应用修复

### 方法 1：重新启动 RimWorld（推荐）

1. **关闭 RimWorld**
2. **重新启动游戏**
3. 新版本的 DLL 会自动加载

### 方法 2：手动更新 DLL（如果游戏正在运行）

如果游戏正在运行且无法关闭：

```powershell
# 1. 关闭 RimWorld
Get-Process -Name "RimWorldWin64" | Stop-Process -Force

# 2. 等待几秒
Start-Sleep -Seconds 3

# 3. 重新启动游戏
Start-Process "D:\steam\steamapps\common\RimWorld\RimWorldWin64.exe"
```

## 验证修复

重启后，打开 **Mod 设置** → **RimTalk-ExpandActions**，应该看到：

```
RimTalk-ExpandActions v1.1.0

7种行为系统已启用（招募/投降/恋爱/灵感/休息/赠送/用餐）
所有行为默认100%成功率
自动注入规则功能已启用

详细设置请编辑配置文件或通过代码调用。
```

**不应该再看到：**
```
? Exception filling window for RimWorld.Dialog_ModSettings...
```

## 当前 DLL 版本信息

| 位置 | 文件大小 | 最后修改时间 |
|------|---------|-------------|
| 工作区 | 81,408 bytes | 2025/12/14 15:32:55 |
| 游戏目录 | 81,408 bytes | 2025/12/14 15:32:55 |

**游戏启动时间：** 2025/12/14 15:33:41

**结论：** 游戏在 DLL 更新后启动，但加载的是启动时的版本。需要重启游戏来加载最新 DLL。

## 技术细节

### RimWorld API 变化

**RimWorld 1.5 及之前：**
```csharp
public Rect Label(string label, float height = -1f, string tooltip = null)
```

**RimWorld 1.6：**
```csharp
public void Label(string label)
```

### 兼容性建议

为了确保跨版本兼容，Mod 设置界面应该：
1. ? 只使用单参数 `Label(string)` 方法
2. ? 使用 `Gap()` 方法来添加空间
3. ? 避免使用高度和提示参数

## 相关提交

- 提交：`61267c2`
- 消息：fix: Update DoSettingsWindowContents for RimWorld 1.6 compatibility
- 日期：2025/12/14 14:51

## 状态

- [x] 代码已修复
- [x] DLL 已编译
- [x] DLL 已部署
- [ ] **需要重启 RimWorld 来应用**
