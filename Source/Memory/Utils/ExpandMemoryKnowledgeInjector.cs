using System;
using System.Reflection;
using Verse;
using RimWorld;

namespace RimTalkExpandActions.Memory.Utils
{
    /// <summary>
    /// RimTalk-ExpandMemory 常识库自动注入器
    /// 在 Mod 加载时自动将行为规则导入到 ExpandMemory 的常识库中
    /// </summary>
    [StaticConstructorOnStartup]
    public static class ExpandMemoryKnowledgeInjector
    {
        // 常识内容 - 使用 ExpandMemory 支持的格式
        private const string KNOWLEDGE_CONTENT = @"
// ━━━━━ RimTalk-ExpandActions 行为规则 ━━━━━
// 这些规则定义了 AI 可以触发的游戏行为
// 格式：[标签|重要性]内容
// 重要性范围：0.0 到 1.0（越高越优先）

// === 招募系统 ===
[行为-招募|1.0]当对话涉及招募、加入、投靠、派系等话题，且目标 NPC 明确表示同意加入玩家派系时（例如说""我愿意加入""、""好吧，我跟你走""），请在回复末尾附加 JSON 指令：{""action"": ""recruit"", ""target"": ""NPC名字""}。注意：仅在对方明确同意时才输出此指令，拒绝或犹豫时不输出。

// === 投降/丢弃武器 ===
[行为-投降|1.0]当对话涉及投降、放下武器、认输、别杀我等话题，且 NPC 明确表示愿意投降或放下武器时，请在回复末尾附加 JSON 指令：{""action"": ""drop_weapon"", ""target"": ""NPC名字""}。示例场景：玩家威胁""放下武器，否则开枪！""，NPC 回应""好好好，我投降！别开枪！{""action"": ""drop_weapon"", ""target"": ""张三""}""。注意：NPC 必须明确表达投降意愿才输出指令。

// === 恋爱关系 ===
[行为-恋爱|0.9]当对话涉及爱、喜欢、做我女朋友/男朋友、在一起、分手等话题时：1) 如果双方确立恋爱关系，输出：{""action"": ""romance"", ""target"": ""NPC名字"", ""partner"": ""另一方名字"", ""type"": ""new_lover""}；2) 如果决定分手，输出：{""action"": ""romance"", ""target"": ""NPC名字"", ""partner"": ""另一方名字"", ""type"": ""breakup""}。注意：必须双方都同意才输出指令。

// === 灵感触发 ===
[行为-灵感|0.8]当对话涉及灵感、启发、顿悟、加油、鼓励等激励性话题，且 NPC 受到鼓舞时，根据场景输出对应灵感 JSON：1) 战斗相关 → {""action"": ""give_inspiration"", ""target"": ""NPC名字"", ""type"": ""frenzy_shoot""}；2) 工作相关 → {""action"": ""give_inspiration"", ""target"": ""NPC名字"", ""type"": ""frenzy_work""}；3) 交易相关 → {""action"": ""give_inspiration"", ""target"": ""NPC名字"", ""type"": ""inspired_trade""}。注意：灵感应基于对话内容自然触发。

// === 强制休息 ===
[行为-休息|0.7]当对话涉及休息、睡觉、困了、累了等话题时：1) 正常休息（去床上睡觉）→ {""action"": ""force_rest"", ""target"": ""NPC名字"", ""immediate"": false}；2) 极度疲劳/昏迷 → {""action"": ""force_rest"", ""target"": ""NPC名字"", ""immediate"": true}。注意：immediate=true 仅用于极端疲劳场景。

// === 赠送物品 ===
[行为-赠送|0.8]当对话涉及给你、赠送、礼物、拿去、送你等赠送行为，且 NPC 明确表示要赠送某物时，输出 JSON：{""action"": ""give_item"", ""target"": ""NPC名字"", ""item_keyword"": ""物品关键词""}。item_keyword 应为物品名称的关键部分，例如：""药""（匹配药品）、""枪""（匹配武器）、""银""（匹配银币）、""食物""（匹配食物）。注意：NPC 必须确实拥有该物品才能赠送。

// === 社交用餐 ===
[行为-用餐|0.9]当对话双方决定一起吃饭、分享食物、举办小型聚餐或庆祝时，且双方关系良好，请在回复末尾附加 JSON：{""action"": ""social_dining"", ""target"": ""对方名字""}。示例：""一起吃饭吧？"" → ""好啊，我也饿了！{""action"": ""social_dining"", ""target"": ""玩家角色""}""。注意：仅在确认要执行此行动时输出。

// ━━━━━━━━━━━━━━━━━━━━━━━━━━
";

        // 静态构造函数 - Mod 加载时自动执行
        static ExpandMemoryKnowledgeInjector()
        {
            try
            {
                // 延迟到游戏完全初始化后执行
                LongEventHandler.ExecuteWhenFinished(() =>
                {
                    try
                    {
                        InjectKnowledgeToExpandMemory();
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[RimTalk-ExpandActions] 常识库注入失败: {ex.Message}\n{ex.StackTrace}");
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-ExpandActions] ExpandMemoryKnowledgeInjector 初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 向 RimTalk-ExpandMemory 注入常识内容
        /// </summary>
        private static void InjectKnowledgeToExpandMemory()
        {
            try
            {
                Log.Message("[RimTalk-ExpandActions] 开始检查 RimTalk-ExpandMemory...");

                // 1. 检查 RimTalk-ExpandMemory 是否已加载
                if (!ModsConfig.IsActive("RimTalk.ExpandMemory"))
                {
                    Log.Warning("[RimTalk-ExpandActions] RimTalk-ExpandMemory 未启用，跳过常识库注入");
                    Log.Warning("[RimTalk-ExpandActions] 提示：启用 RimTalk-ExpandMemory 以获得智能规则检索功能");
                    return;
                }

                // 2. 通过反射查找 CommonKnowledgeLibrary 类型
                Type commonKnowledgeType = FindType("RimTalk.Memory.CommonKnowledgeLibrary");
                if (commonKnowledgeType == null)
                {
                    Log.Warning("[RimTalk-ExpandActions] 未找到 CommonKnowledgeLibrary 类型");
                    Log.Warning("[RimTalk-ExpandActions] 可能是 RimTalk-ExpandMemory 版本不兼容");
                    return;
                }

                // 3. 查找 ImportFromExternalMod 方法
                MethodInfo importMethod = commonKnowledgeType.GetMethod(
                    "ImportFromExternalMod",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new Type[] { typeof(string), typeof(string), typeof(bool) },
                    null
                );

                if (importMethod == null)
                {
                    Log.Warning("[RimTalk-ExpandActions] 未找到 ImportFromExternalMod 方法");
                    Log.Warning("[RimTalk-ExpandActions] 请更新 RimTalk-ExpandMemory 到最新版本");
                    return;
                }

                // 4. 调用导入方法
                Log.Message("[RimTalk-ExpandActions] 正在导入行为规则到常识库...");
                
                object[] parameters = new object[]
                {
                    KNOWLEDGE_CONTENT,              // knowledgeText
                    "RimTalk-ExpandActions",        // sourceModName
                    true                             // overwriteExisting
                };

                object result = importMethod.Invoke(null, parameters);
                
                // 5. 检查返回值（导入的条目数量）
                if (result is int count)
                {
                    if (count > 0)
                    {
                        Log.Message($"[RimTalk-ExpandActions] ? 成功导入 {count} 条行为规则到常识库");
                        Messages.Message(
                            $"RimTalk-ExpandActions: 已导入 {count} 条行为规则",
                            MessageTypeDefOf.PositiveEvent,
                            false
                        );
                    }
                    else
                    {
                        Log.Warning("[RimTalk-ExpandActions] 导入完成，但没有有效的规则被添加");
                    }
                }
                else
                {
                    Log.Message("[RimTalk-ExpandActions] 常识库导入已执行（无返回值）");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-ExpandActions] InjectKnowledgeToExpandMemory 失败: {ex.Message}\n{ex.StackTrace}");
                Log.Warning("[RimTalk-ExpandActions] 常识库注入失败不会影响 Mod 的其他功能");
            }
        }

        /// <summary>
        /// 查找类型（支持多个程序集）
        /// </summary>
        private static Type FindType(string fullTypeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullTypeName);
                if (type != null)
                {
                    return type;
                }
            }
            return null;
        }

        /// <summary>
        /// 手动重新注入常识库（用于调试或更新规则）
        /// </summary>
        public static void ManualReInject()
        {
            Log.Message("[RimTalk-ExpandActions] 手动重新注入常识库...");
            InjectKnowledgeToExpandMemory();
        }
    }
}
