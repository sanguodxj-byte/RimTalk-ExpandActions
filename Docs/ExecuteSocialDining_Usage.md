# ExecuteSocialDining 方法使用文档

## ?? 方法签名

```csharp
public static void ExecuteSocialDining(Pawn initiator, Pawn target)
```

## ?? 功能说明

通过代码触发社交用餐行为，让 `initiator`（发起者）邀请 `target`（目标）一起共进晚餐。

## ?? 实现逻辑

### 1. **参数验证**
- 检查 `initiator` 和 `target` 是否为 null
- 检查两者是否存活且未倒地
- 检查两者是否在同一地图上

### 2. **食物查找**
使用 `FoodSharingUtility.TryFindSharedFood(initiator, target, out Thing food)` 查找可用食物：
- 优先从发起者背包中查找
- 其次在地图上查找可达的食物
- 食物必须未被预订且可食用

### 3. **任务创建**
```csharp
Job job = JobMaker.MakeJob(SocialDiningDefOf.SocialDine, food, null, target);
job.count = 2; // 需要 2 份食物
```

### 4. **座位查找（可选）**
使用 `FoodSharingUtility.TryFindChair(initiator, out Building chair)` 查找最佳座位：
- 优先选择餐椅（`isDiningChair`）
- 靠近餐桌的座位会获得加分
- 距离越近越好

### 5. **任务分配**
```csharp
initiator.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc));
```

### 6. **玩家提示**
发送消息通知玩家："Xx 邀请 Xx 一起共进晚餐。"

## ?? 使用示例

### 示例 1：AI 对话触发

在 `AIResponsePostProcessor.cs` 中添加新的 action 处理：

```csharp
case "social_dining":
    HandleSocialDiningAction(jsonBlock, targetPawn);
    break;

// ...

private static void HandleSocialDiningAction(string jsonBlock, Pawn targetPawn)
{
    string initiatorName = ExtractJsonField(jsonBlock, "initiator");
    string recipientName = ExtractJsonField(jsonBlock, "recipient");

    Pawn initiator = FindPawnByName(initiatorName);
    Pawn recipient = FindPawnByName(recipientName);

    if (initiator != null && recipient != null)
    {
        Log.Message($"[RimTalk-ExpandActions] 检测到社交用餐指令: {initiator.Name} -> {recipient.Name}");
        RimTalkActions.ExecuteSocialDining(initiator, recipient);
    }
}
```

### 示例 2：开发者控制台直接调用

```csharp
// 在开发者控制台执行
using RimTalkExpandActions.Memory.Actions;

Pawn pawn1 = Find.CurrentMap.mapPawns.FreeColonists[0];
Pawn pawn2 = Find.CurrentMap.mapPawns.FreeColonists[1];

RimTalkActions.ExecuteSocialDining(pawn1, pawn2);
```

### 示例 3：从事件或剧本触发

```csharp
public class SocialDiningIncident : IncidentWorker
{
    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        Map map = (Map)parms.target;
        
        // 随机选择两个殖民者
        var colonists = map.mapPawns.FreeColonists.Where(p => 
            !p.Dead && !p.Downed && p.Spawned).ToList();
        
        if (colonists.Count < 2)
            return false;
            
        Pawn initiator = colonists.RandomElement();
        Pawn target = colonists.Where(p => p != initiator).RandomElement();
        
        RimTalkActions.ExecuteSocialDining(initiator, target);
        
        return true;
    }
}
```

## ?? 注意事项

### 前置条件
1. **食物可用性**
   - 发起者背包或地图上必须有可食用的食物
   - 食物未被其他小人预订
   - 食物数量至少为 1（最好 ≥2）

2. **角色状态**
   - 两个角色都必须存活
   - 两个角色都必须在地图上（Spawned）
   - 两个角色必须在同一地图
   - 两个角色都不能倒地（Downed）

3. **任务系统**
   - 发起者的 `jobs` 组件必须可用
   - 发起者当前没有更高优先级的不可中断任务

### 失败情况处理

| 失败原因 | 日志级别 | 是否显示消息 |
|---------|---------|-------------|
| 参数为 null | Error | 否 |
| 角色已死亡 | Warning | 否 |
| 不在同一地图 | Warning | 否 |
| 找不到食物 | Message | DevMode 时显示 |
| 任务分配失败 | Warning | 否 |

## ?? 执行流程图

```
开始
  ↓
参数验证
  ↓
┌─ 检查是否为 null
├─ 检查是否存活
├─ 检查是否在地图上
└─ 检查是否在同一地图
  ↓
查找可用食物
  ↓
┌─ 从背包查找
└─ 从地图查找
  ↓
创建社交用餐任务
  ↓
查找座位（可选）
  ↓
标记食物为共享
  ↓
分配任务给发起者
  ↓
发送玩家消息
  ↓
结束
```

## ?? 测试检查清单

- [ ] ? 编译通过
- [ ] ? 两个殖民者在同一地图
- [ ] ? 有可用食物（背包或地图）
- [ ] ? 发起者成功前往食物位置
- [ ] ? 发起者拾取食物
- [ ] ? 发起者前往座位（如果有）
- [ ] ? 发起者与目标一起进餐
- [ ] ? 获得 "与人共餐" 心情加成 (+3)
- [ ] ? 玩家收到提示消息

## ?? 相关类和方法

- `RimTalkExpandActions.SocialDining.FoodSharingUtility`
  - `TryFindSharedFood()` - 查找可共享食物
  - `TryFindChair()` - 查找座位
  - `MarkFoodAsShared()` - 标记食物
  
- `RimTalkExpandActions.SocialDining.SocialDiningDefOf`
  - `SocialDine` - 社交用餐任务定义
  
- `RimTalkExpandActions.SocialDining.JobDriver_SocialDine`
  - 社交用餐任务的具体执行逻辑

## ?? 版本历史

- **v1.0** (2025-12-14)
  - 首次实现 `ExecuteSocialDining` 方法
  - 支持自动查找食物和座位
  - 集成到 `RimTalkActions` 静态工具类
