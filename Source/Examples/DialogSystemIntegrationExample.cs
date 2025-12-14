using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;
using RimTalkExpandActions.Memory;
using RimTalkExpandActions.Memory.Actions;

namespace RimTalkExpandActions.Examples
{
    /// <summary>
    /// 使用示例：展示如何在对话系统中集成招募功能
    /// 这是一个示例类，展示集成方式，实际使用时需要适配您的对话系统
    /// </summary>
    public class DialogSystemIntegrationExample
    {
        /// <summary>
        /// 示例 1: 基础集成 - 在收到 AI 回复后处理
        /// </summary>
        public void OnReceiveAIResponse(string aiResponseText, Pawn targetNPC, Pawn playerColonist)
        {
            try
            {
                // 使用后处理器处理 AI 回复
                // 这会自动检测并执行动作指令，同时返回清理后的文本
                string cleanText = AIResponsePostProcessor.ProcessActionResponse(
                    aiResponseText,
                    targetNPC,
                    playerColonist
                );

                // 显示清理后的文本给玩家（具体实现取决于您的对话 UI）
                ShowDialogBubble(targetNPC, cleanText);

                Log.Message($"[示例] 处理 AI 回复完成: {cleanText}");
            }
            catch (Exception ex)
            {
                Log.Error($"[示例] OnReceiveAIResponse 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 示例 2: 构建 AI 提示词时包含系统规则
        /// </summary>
        public string BuildAIPrompt(Pawn targetNPC, string playerMessage)
        {
            StringBuilder promptBuilder = new StringBuilder();

            try
            {
                // 1. 添加基础对话上下文
                promptBuilder.AppendLine($"你是 {targetNPC.Name.ToStringShort}，一个 {targetNPC.def.label}。");
                promptBuilder.AppendLine($"当前派系: {targetNPC.Faction?.Name ?? "无派系"}");
                promptBuilder.AppendLine();

                // 2. 从常识库获取系统规则（包括招募规则）
                var memoryManager = MemoryManager.Instance;
                if (memoryManager?.CommonKnowledge != null)
                {
                    var systemRules = memoryManager.CommonKnowledge.GetAllEntries();
                    
                    promptBuilder.AppendLine("=== 系统指令 ===");
                    foreach (var rule in systemRules)
                    {
                        if (rule.Tag == "系统指令" && rule.Importance >= 0.8f)
                        {
                            promptBuilder.AppendLine(rule.Content);
                            promptBuilder.AppendLine();
                        }
                    }
                }

                // 3. 添加玩家消息
                promptBuilder.AppendLine("=== 对话 ===");
                promptBuilder.AppendLine($"玩家: {playerMessage}");
                promptBuilder.AppendLine($"{targetNPC.Name.ToStringShort}: ");

                return promptBuilder.ToString();
            }
            catch (Exception ex)
            {
                Log.Error($"[示例] BuildAIPrompt 失败: {ex.Message}");
                return $"玩家: {playerMessage}\n回复: ";
            }
        }

        /// <summary>
        /// 示例 3: 手动触发招募（不依赖 AI）
        /// </summary>
        public void ManualRecruitment(Pawn targetNPC, Pawn recruiter)
        {
            try
            {
                // 可以添加额外的前置检查
                if (targetNPC.Faction == Faction.OfPlayer)
                {
                    Messages.Message("这个角色已经是你的殖民者了！", MessageTypeDefOf.RejectInput);
                    return;
                }

                if (targetNPC.HostileTo(Faction.OfPlayer))
                {
                    Messages.Message("无法招募敌对派系成员！", MessageTypeDefOf.RejectInput);
                    return;
                }

                // 直接调用招募方法
                RimTalkActions.ExecuteRecruit(targetNPC, recruiter);
            }
            catch (Exception ex)
            {
                Log.Error($"[示例] ManualRecruitment 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 示例 4: 检查是否可以尝试招募
        /// </summary>
        public bool CanAttemptRecruitment(Pawn targetNPC)
        {
            if (targetNPC == null || targetNPC.Dead)
                return false;

            if (targetNPC.Faction == Faction.OfPlayer)
                return false; // 已经是殖民者

            if (targetNPC.Faction != null && targetNPC.Faction.HostileTo(Faction.OfPlayer))
                return false; // 敌对派系

            if (targetNPC.IsPrisoner)
                return false; // 囚犯使用游戏原生的招募系统

            return true;
        }

        /// <summary>
        /// 示例 5: 获取招募相关的常识条目（用于调试）
        /// </summary>
        public void DebugShowRecruitmentRules()
        {
            try
            {
                var commonKnowledge = MemoryManager.Instance?.CommonKnowledge;
                if (commonKnowledge == null)
                {
                    Log.Warning("[示例] CommonKnowledge 未初始化");
                    return;
                }

                var recruitRule = commonKnowledge.GetEntryById("sys-rule-recruit");
                if (recruitRule != null)
                {
                    Log.Message($"[示例] 招募规则已加载:");
                    Log.Message($"  - ID: {recruitRule.Id}");
                    Log.Message($"  - Tag: {recruitRule.Tag}");
                    Log.Message($"  - Importance: {recruitRule.Importance}");
                    Log.Message($"  - Keywords: {recruitRule.Keywords}");
                }
                else
                {
                    Log.Warning("[示例] 招募规则未找到，请检查 RuleInjector 是否正常运行");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[示例] DebugShowRecruitmentRules 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 示例 6: 测试 JSON 解析
        /// </summary>
        public void TestJsonParsing()
        {
            var testCases = new[]
            {
                "好的，我愿意加入！{\"action\": \"recruit\", \"target\": \"艾莉丝\"}",
                "让我考虑一下...",
                "我同意了！{ \"action\": \"recruit\", \"target\": \"张三\" }",
            };

            foreach (var testCase in testCases)
            {
                if (AIResponsePostProcessor.TryParseActionJson(testCase, out string action, out string target))
                {
                    Log.Message($"[测试] 解析成功 - Action: {action}, Target: {target}");
                }
                else
                {
                    Log.Message($"[测试] 无动作指令: {testCase}");
                }
            }
        }

        // 占位方法 - 实际实现取决于您的对话 UI 系统
        private void ShowDialogBubble(Pawn speaker, string text)
        {
            // 这里应该调用您的对话气泡显示逻辑
            // 例如: DialogBubbleManager.Show(speaker, text);
            
            // 临时使用游戏消息代替
            Messages.Message($"{speaker.Name.ToStringShort}: {text}", speaker, MessageTypeDefOf.SilentInput);
        }
    }

    /// <summary>
    /// 示例：Harmony 补丁，展示如何拦截对话流程
    /// 需要引用 HarmonyLib (0Harmony.dll)
    /// </summary>
    /*
    [HarmonyPatch(typeof(YourDialogClass), "ProcessAIResponse")]
    public static class Patch_ProcessAIResponse
    {
        static void Postfix(string __result, Pawn targetNPC, Pawn player)
        {
            try
            {
                // 在原方法执行后，处理动作指令
                string cleanText = AIResponsePostProcessor.ProcessActionResponse(
                    __result,
                    targetNPC,
                    player
                );
                
                // 如果需要修改返回值，使用 ref string __result
                // __result = cleanText;
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-ExpandActions] Patch_ProcessAIResponse 失败: {ex.Message}");
            }
        }
    }
    */
}
