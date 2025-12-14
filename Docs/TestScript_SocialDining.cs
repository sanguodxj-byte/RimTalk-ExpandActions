// 快速测试 ExecuteSocialDining 方法
// 在 RimWorld 开发者控制台中执行此代码

using RimTalkExpandActions.Memory.Actions;
using Verse;
using System.Linq;

// 获取当前地图
var map = Find.CurrentMap;
if (map == null)
{
    Log.Error("没有活动地图！");
    return;
}

// 获取至少 2 个自由殖民者
var colonists = map.mapPawns.FreeColonists
    .Where(p => !p.Dead && !p.Downed && p.Spawned)
    .ToList();

if (colonists.Count < 2)
{
    Log.Error($"殖民者数量不足（需要至少 2 个，当前 {colonists.Count} 个）");
    return;
}

// 选择前两个殖民者
Pawn initiator = colonists[0];
Pawn target = colonists[1];

Log.Message($"测试社交用餐功能:");
Log.Message($"  发起者: {initiator.Name.ToStringShort}");
Log.Message($"  目标: {target.Name.ToStringShort}");

// 执行社交用餐
RimTalkActions.ExecuteSocialDining(initiator, target);

Log.Message("社交用餐任务已触发！观察游戏中的行为。");
