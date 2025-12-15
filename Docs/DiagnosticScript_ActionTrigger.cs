using System;
using System.Linq;
using Verse;
using RimWorld;
using RimTalkExpandActions.Memory;
using RimTalkExpandActions.Memory.Actions;

/// <summary>
/// 行为触发诊断脚本
/// 
/// 使用方法：
/// 1. 启用开发者模式 (Dev Mode)
/// 2. 按 F12 打开开发者控制台
/// 3. 将此脚本内容粘贴到控制台
/// 4. 查看日志输出
/// 
/// 此脚本会测试：
/// - JSON 解析是否正常
/// - 行为开关是否启用
/// - Pawn 查找是否工作
/// - 延迟执行是否正常
/// - 实际行为执行是否成功
/// </summary>
public static class ActionTriggerDiagnostic
{
    public static void RunDiagnostic()
    {
        Log.Message("═══════════════════════════════════════");
        Log.Message("RimTalk-ExpandActions 行为触发诊断");
        Log.Message("═══════════════════════════════════════");
        
        // 测试 1：JSON 解析
        TestJsonParsing();
        
        // 测试 2：行为开关
        TestActionSettings();
        
        // 测试 3：Pawn 查找
        TestPawnFinding();
        
        // 测试 4：延迟执行
        TestDeferredExecution();
        
        // 测试 5：实际行为
        TestActualExecution();
        
        Log.Message("\n═══════════════════════════════════════");
        Log.Message("诊断完成 - 请查看上方结果");
        Log.Message("═══════════════════════════════════════");
    }
    
    private static void TestJsonParsing()
    {
        Log.Message("\n[1/5] 测试 JSON 解析");
        Log.Message("────────────────────────────────");
        
        string[] testCases = {
            "好啊！{\"action\": \"recruit\", \"target\": \"TestPawn\"}",
            "我们一起吃饭吧！{\"action\": \"social_dining\", \"target\": \"Alice\"}",
            "放松一下{\"action\": \"social_relax\", \"targets\": \"Val,Cait\"}"
        };
        
        foreach (var test in testCases)
        {
            try
            {
                string cleaned = AIResponsePostProcessor.ProcessActionResponse(test, null, null);
                bool detected = cleaned != test;
                Log.Message($"测试: {test.Substring(0, Math.Min(30, test.Length))}...");
                Log.Message($"  结果: {(detected ? "? JSON 被检测并移除" : "? JSON 未被检测")}");
            }
            catch (Exception ex)
            {
                Log.Error($"  ? 异常: {ex.Message}");
            }
        }
    }
    
    private static void TestActionSettings()
    {
        Log.Message("\n[2/5] 测试行为开关和成功率");
        Log.Message("────────────────────────────────");
        
        var settings = RimTalkExpandActionsMod.Settings;
        if (settings == null)
        {
            Log.Error("  ? Mod 设置未加载！");
            return;
        }
        
        string[] actions = { 
            "recruit", "social_dining", "social_relax", 
            "romance", "give_inspiration", "force_rest", 
            "give_item", "drop_weapon" 
        };
        
        int enabledCount = 0;
        foreach (var action in actions)
        {
            bool enabled = settings.IsActionEnabled(action);
            float chance = settings.GetSuccessChance(action);
            
            string status = enabled ? "? 启用" : "? 禁用";
            string chanceStr = $"{chance:P0}";
            
            Log.Message($"  {action.PadRight(20)}: {status} | 成功率: {chanceStr}");
            
            if (enabled) enabledCount++;
        }
        
        Log.Message($"\n  总结: {enabledCount}/{actions.Length} 个行为已启用");
    }
    
    private static void TestPawnFinding()
    {
        Log.Message("\n[3/5] 测试 Pawn 查找");
        Log.Message("────────────────────────────────");
        
        if (Find.Maps == null || Find.Maps.Count == 0)
        {
            Log.Warning("  ? 没有加载的地图");
            return;
        }
        
        var map = Find.Maps[0];
        var colonists = map.mapPawns.FreeColonists;
        
        if (!colonists.Any())
        {
            Log.Warning("  ? 地图上没有殖民者");
            return;
        }
        
        Log.Message($"  找到 {colonists.Count()} 个殖民者");
        
        foreach (var pawn in colonists.Take(5))
        {
            var nameTriple = pawn.Name as NameTriple;
            string nick = nameTriple?.Nick ?? "无昵称";
            string fullName = pawn.Name.ToStringFull;
            string shortName = pawn.Name.ToStringShort;
            
            Log.Message($"  - {shortName}");
            Log.Message($"      全名: {fullName}");
            Log.Message($"      昵称: {nick}");
            Log.Message($"      派系: {pawn.Faction?.Name ?? "无"}");
            Log.Message($"      状态: {(pawn.Spawned ? "在地图上" : "不在地图上")}");
        }
    }
    
    private static void TestDeferredExecution()
    {
        Log.Message("\n[4/5] 测试延迟执行机制");
        Log.Message("────────────────────────────────");
        
        int testValue = 0;
        
        try
        {
            LongEventHandler.ExecuteWhenFinished(() => {
                testValue = 42;
                Log.Message($"  ? 延迟回调成功执行，testValue 已更新为 {testValue}");
            });
            
            Log.Message($"  当前 testValue = {testValue}");
            Log.Message($"  (应该是 0，下一帧会变成 42)");
            Log.Message($"  ? LongEventHandler.ExecuteWhenFinished 调用成功");
        }
        catch (Exception ex)
        {
            Log.Error($"  ? 延迟执行失败: {ex.Message}");
        }
    }
    
    private static void TestActualExecution()
    {
        Log.Message("\n[5/5] 测试实际行为执行");
        Log.Message("────────────────────────────────");
        
        if (Find.Maps == null || Find.Maps.Count == 0 || !Find.Maps[0].mapPawns.FreeColonists.Any())
        {
            Log.Warning("  ? 无法测试：没有可用的殖民者");
            return;
        }
        
        var pawn = Find.Maps[0].mapPawns.FreeColonists.First();
        Log.Message($"  测试对象: {pawn.Name.ToStringShort}");
        
        // 测试社交放松
        try
        {
            Log.Message($"\n  执行 ExecuteSocialRelax...");
            bool result = RimTalkActions.ExecuteSocialRelax(pawn, pawn.Name.ToStringShort);
            
            if (result)
            {
                Log.Message($"  ? ExecuteSocialRelax 返回成功");
                Log.Message($"  (应该看到游戏消息：1 名小人开始社交放松)");
            }
            else
            {
                Log.Warning($"  ? ExecuteSocialRelax 返回失败");
                Log.Warning($"  可能原因：成功率检定失败、Pawn 状态不满足条件等");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"  ? ExecuteSocialRelax 抛出异常: {ex.Message}");
            Log.Error($"  堆栈: {ex.StackTrace}");
        }
    }
}

// ═══════════════════════════════════════
// 运行诊断
// ═══════════════════════════════════════
ActionTriggerDiagnostic.RunDiagnostic();
