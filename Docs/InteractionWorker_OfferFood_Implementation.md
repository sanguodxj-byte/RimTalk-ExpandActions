# InteractionWorker_OfferFood 实现说明

## ?? 功能概述

这个 `InteractionWorker_OfferFood` 实现了 **双方立即开始吃饭** 的社交共餐功能。

当 A 邀请 B 吃饭时：
1. ? 自动查找最佳食物（背包 → 地图）
2. ? 为双方创建社交用餐任务
3. ? 使用 `TryTakeOrderedJob` 强制指派任务
4. ? 双方同时开始吃饭

---

## ?? 核心方法

### 1. `Interacted` - 交互触发

```csharp
public override void Interacted(Pawn initiator, Pawn recipient, ...)
```

**流程：**
1. 添加心情 buff（OfferedFood / ReceivedFoodOffer）
2. 调用 `TryStartSocialDining` 开始共餐

---

### 2. `TryStartSocialDining` - 核心逻辑

```csharp
private bool TryStartSocialDining(Pawn initiator, Pawn recipient)
```

**步骤：**

| 步骤 | 操作 | 说明 |
|------|------|------|
| 1 | `FindBestFoodForDining` | 查找最佳食物 |
| 2 | 放下持有的食物 | 如果发起者持有食物 |
| 3 | 验证食物有效性 | 检查是否被销毁 |
| 4 | `CanBothPawnsReserveFood` | **多人预留检查** |
| 5 | `TryFindTableForTwo` | 查找餐桌（可选） |
| 6 | `CreateDiningJob` × 2 | 创建两个任务 |
| 7 | `StartDiningJob` × 2 | **强制指派任务** |
| 8 | 失败处理 | 取消已开始的任务 |
| 9 | `MarkFoodAsShared` | 标记食物为共享 |

---

### 3. `FindBestFoodForDining` - 食物查找

```csharp
private Thing FindBestFoodForDining(Pawn pawn1, Pawn pawn2)
```

**优先级：**

1. **发起者背包** → `FindFoodInInventory(pawn1)`
2. **接收者背包** → `FindFoodInInventory(pawn2)`
3. **地图上最近** → `FindFoodOnMap(pawn1, pawn2)`

**食物评分公式：**
```csharp
score = nutrition + (preferability * 10)
```

---

### 4. `CanBothPawnsReserveFood` - 预留检查

```csharp
private bool CanBothPawnsReserveFood(Pawn pawn1, Pawn pawn2, Thing food)
```

**关键逻辑：** 多人共餐核心

```csharp
// 检查 pawn1
if (!pawn1.CanReserve(food, 1, -1, null, false))
{
    Pawn reserver = pawn1.Map?.reservationManager?.FirstRespectedReserver(food, pawn1);
    if (reserver != pawn2)
    {
        return false; // 被第三方预留
    }
}

// 检查 pawn2（同理）
```

**说明：**
- ? 允许 pawn1 和 pawn2 互相预留同一食物
- ? 拒绝第三方预留

---

### 5. `StartDiningJob` - 强制指派任务

```csharp
private bool StartDiningJob(Pawn pawn, Job job)
{
    return pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc, false);
}
```

**关键参数：**
- `JobTag.Misc` - 杂项任务
- `false` - 不请求最后一次机会

---

## ?? 食物验证流程

### `IsFoodValidForDining`

```csharp
private bool IsFoodValidForDining(Pawn pawn, Thing food)
```

**检查项：**

| 检查 | 条件 |
|------|------|
| ? 有效性 | `food != null && food.def != null` |
| ? 可食用 | `food.def.IsIngestible` |
| ? 现在可吃 | `food.IngestibleNow` |
| ? 未被禁止 | `!food.IsForbidden(pawn)` |
| ? 未被他人共享 | `tracker.ActiveEatersCount < 2` |

---

## ?? 使用示例

### 示例 1：手动触发

```csharp
// 在开发者控制台
var pawn1 = Find.Maps[0].mapPawns.FreeColonists.First();
var pawn2 = Find.Maps[0].mapPawns.FreeColonists.Skip(1).First();

// 触发交互
InteractionDefOf.OfferFood.Worker.Interacted(
    pawn1, pawn2, 
    null, 
    out string text, out string label, out LetterDef def, out LookTargets targets
);
```

### 示例 2：通过 RimTalk

```csharp
// AI 输出
"好啊！我们一起吃饭吧！{\"action\": \"social_dining\", \"target\": \"Bob\"}"

// 自动调用
RimTalkActions.ExecuteSocialDining(initiator, "Bob");
  → 触发 InteractionWorker_OfferFood.Interacted
  → 双方开始吃饭
```

---

## ?? 配置选项

### 搜索半径

在 `FoodSharingUtility` 中定义：

```csharp
private const float FoodSearchRadius = 45f;    // 食物搜索半径
private const float TableSearchRadius = 40f;   // 餐桌搜索半径
```

### 随机触发权重

在 `InteractionWorker_OfferFood` 中：

```csharp
public override float RandomSelectionWeight(...)
{
    return 0.02f; // 2% 概率自动触发
}
```

---

## ?? 常见问题

### Q1: 为什么一方开始吃饭，另一方没有？

**原因：** 预留冲突或任务指派失败

**解决：**
1. 检查 `CanBothPawnsReserveFood` 返回值
2. 查看日志：
   ```
   [SocialDining] Bob 无法接受社交共餐任务
   ```

### Q2: 为什么找不到食物？

**原因：** 背包和地图上都没有有效食物

**检查：**
1. 背包中是否有食物
2. 地图上是否有未被禁止的食物
3. 食物是否已被其他两人共享

### Q3: 为什么任务立即取消？

**原因：** 双方必须同时成功指派

**逻辑：**
```csharp
if (!initiatorStarted || !recipientStarted)
{
    // 取消已成功的任务
    if (initiatorStarted)
    {
        initiator.jobs.EndCurrentJob(JobCondition.InterruptForced);
    }
    if (recipientStarted)
    {
        recipient.jobs.EndCurrentJob(JobCondition.InterruptForced);
    }
    return false;
}
```

---

## ?? 性能优化

### 食物查找

- ? 优先背包（O(n)，n 通常 < 10）
- ? 使用 `GenClosest.ClosestThingReachable`（寻路优化）
- ? 缓存中点位置

### 预留检查

- ? 提前失败（快速返回）
- ? 只检查必要条件

---

## ?? 自定义扩展

### 添加新的食物偏好

```csharp
private float GetFoodQualityScore(Thing food)
{
    float nutrition = food.GetStatValue(StatDefOf.Nutrition, true);
    float preferability = (float)(food.def.ingestible?.preferability ?? FoodPreferability.RawBad);
    
    // 添加自定义评分
    float customBonus = 0f;
    if (food.def.defName == "MealLavish")
    {
        customBonus = 50f; // 豪华餐加分
    }
    
    return nutrition + (preferability * 10f) + customBonus;
}
```

### 修改搜索半径

```csharp
// 在 FindFoodOnMap 中
return GenClosest.ClosestThingReachable(
    midPoint,
    pawn1.Map,
    request,
    PathEndMode.Touch,
    traverseParms,
    60f, // 修改为 60 格
    t => IsFoodValidForDining(pawn1, t) && IsFoodValidForDining(pawn2, t)
);
```

---

## ? 测试清单

| 测试项 | 检查 |
|--------|------|
| ? 双方开始吃饭 | 两个 Pawn 同时进入 SocialDine 任务 |
| ? 背包食物优先 | 使用背包中的食物 |
| ? 地图食物查找 | 背包无食物时查找地图 |
| ? 预留冲突处理 | 第三方预留时取消 |
| ? 失败回滚 | 一方失败时取消另一方 |
| ? 食物标记 | `SharedFoodTracker` 正确记录 |
| ? 心情buff | OfferedFood 和 ReceivedFoodOffer |

---

## ?? 相关文件

- `Source/SocialDining/InteractionWorker_OfferFood.cs` - 本文件
- `Source/SocialDining/FoodSharingUtility.cs` - 工具类
- `Source/SocialDining/JobDriver_SocialDine.cs` - 任务驱动
- `Source/SocialDining/SharedFoodTracker.cs` - 共享追踪
- `Defs/InteractionDefs/Interaction_OfferFood.xml` - 交互定义
- `Defs/JobDefs/Jobs_SocialDining.xml` - 任务定义

---

**最后更新：** 2025/12/15  
**版本：** v2.0 (增强版)  
**作者：** RimTalk-ExpandActions
