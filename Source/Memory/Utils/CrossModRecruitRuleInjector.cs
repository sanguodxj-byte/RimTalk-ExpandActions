using System;
using System.Linq;
using System.Reflection;
using Verse;
using RimWorld;

namespace RimTalkExpandActions.Memory.Utils
{
    /// <summary>
    /// 跨项目招募规则注入器
    /// 通过反射向 RimTalk-ExpandMemory 的常识库注入规则
    /// </summary>
    public static class CrossModRecruitRuleInjector
    {
        private const string RECRUIT_RULE_ID = "expand-action-recruit";
        private const string RECRUIT_RULE_TAG = "招募|加入|投靠|派系|收留|收编|归顺";
        
        private const string TARGET_MOD_NAMESPACE = "RimTalk.Memory";
        private const string MEMORY_MANAGER_TYPE = "MemoryManager";
        private const string COMMON_KNOWLEDGE_TYPE = "CommonKnowledgeLibrary";
        private const string KNOWLEDGE_ENTRY_TYPE = "CommonKnowledgeEntry";

        /// <summary>
        /// 尝试向 RimTalk-ExpandMemory 注入招募规则
        /// </summary>
        /// <param name="importance">规则重要性 (0.0 - 1.0)</param>
        /// <param name="customContent">自定义规则内容（可选）</param>
        /// <returns>是否注入成功</returns>
        public static bool TryInjectRecruitRule(float importance = 1.0f, string customContent = null)
        {
            try
            {
                Log.Message("[RimTalk-ExpandActions] 开始尝试向 RimTalk-ExpandMemory 注入招募规则...");

                // 1. 查找 MemoryManager 类型
                Type memoryManagerType = FindType($"{TARGET_MOD_NAMESPACE}.{MEMORY_MANAGER_TYPE}");
                if (memoryManagerType == null)
                {
                    Log.Warning("[RimTalk-ExpandActions] 未找到 RimTalk.Memory.MemoryManager 类型，请确认 RimTalk-ExpandMemory Mod 已加载");
                    return false;
                }

                // 2. 获取 CommonKnowledge 实例
                object commonKnowledge = GetCommonKnowledgeInstance(memoryManagerType);
                if (commonKnowledge == null)
                {
                    Log.Error("[RimTalk-ExpandActions] 无法获取 CommonKnowledge 实例");
                    return false;
                }

                // 3. 检查规则是否已存在
                if (CheckIfRuleExists(commonKnowledge))
                {
                    Log.Message("[RimTalk-ExpandActions] 招募规则已存在，跳过注入");
                    return true; // 已存在算成功
                }

                // 4. 创建新的招募规则条目
                object recruitRule = CreateRecruitRuleEntry(importance, customContent);
                if (recruitRule == null)
                {
                    Log.Error("[RimTalk-ExpandActions] 创建招募规则条目失败");
                    return false;
                }

                // 5. 添加到常识库
                bool added = AddEntryToCommonKnowledge(commonKnowledge, recruitRule);
                if (added)
                {
                    Log.Message("[RimTalk-ExpandActions] ? 成功向 RimTalk-ExpandMemory 注入招募规则！");
                    Messages.Message("招募规则已成功注入到 RimTalk 常识库！", MessageTypeDefOf.PositiveEvent);
                    return true;
                }
                else
                {
                    Log.Error("[RimTalk-ExpandActions] 添加招募规则到常识库失败");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-ExpandActions] 注入招募规则失败: {ex.Message}\n{ex.StackTrace}");
                Messages.Message("注入招募规则失败，请查看日志", MessageTypeDefOf.RejectInput);
                return false;
            }
        }

        /// <summary>
        /// 移除招募规则
        /// </summary>
        public static bool TryRemoveRecruitRule()
        {
            try
            {
                Type memoryManagerType = FindType($"{TARGET_MOD_NAMESPACE}.{MEMORY_MANAGER_TYPE}");
                if (memoryManagerType == null) return false;

                object commonKnowledge = GetCommonKnowledgeInstance(memoryManagerType);
                if (commonKnowledge == null) return false;

                // 调用 RemoveEntry(id) 方法
                MethodInfo removeMethod = commonKnowledge.GetType().GetMethod("RemoveEntry");
                if (removeMethod != null)
                {
                    removeMethod.Invoke(commonKnowledge, new object[] { RECRUIT_RULE_ID });
                    Log.Message("[RimTalk-ExpandActions] 招募规则已移除");
                    Messages.Message("招募规则已从常识库移除", MessageTypeDefOf.NeutralEvent);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-ExpandActions] 移除招募规则失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查招募规则是否已存在
        /// </summary>
        public static bool CheckIfRecruitRuleExists()
        {
            try
            {
                Type memoryManagerType = FindType($"{TARGET_MOD_NAMESPACE}.{MEMORY_MANAGER_TYPE}");
                if (memoryManagerType == null) return false;

                object commonKnowledge = GetCommonKnowledgeInstance(memoryManagerType);
                if (commonKnowledge == null) return false;

                return CheckIfRuleExists(commonKnowledge);
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-ExpandActions] 检查规则是否存在失败: {ex.Message}");
                return false;
            }
        }

        #region 反射辅助方法

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
        /// 获取 CommonKnowledge 实例
        /// </summary>
        private static object GetCommonKnowledgeInstance(Type memoryManagerType)
        {
            try
            {
                // 尝试通过 静态方法 获取
                MethodInfo getCommonKnowledgeMethod = memoryManagerType.GetMethod(
                    "GetCommonKnowledge",
                    BindingFlags.Public | BindingFlags.Static
                );

                if (getCommonKnowledgeMethod != null)
                {
                    return getCommonKnowledgeMethod.Invoke(null, null);
                }

                // 尝试通过 WorldComponent 获取
                if (Current.Game != null)
                {
                    var world = Find.World;
                    MethodInfo getComponentMethod = world.GetType().GetMethod("GetComponent")
                        .MakeGenericMethod(memoryManagerType);
                    
                    object memoryManager = getComponentMethod.Invoke(world, null);
                    if (memoryManager != null)
                    {
                        PropertyInfo commonKnowledgeProp = memoryManagerType.GetProperty("CommonKnowledge");
                        if (commonKnowledgeProp != null)
                        {
                            return commonKnowledgeProp.GetValue(memoryManager);
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-ExpandActions] GetCommonKnowledgeInstance 失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 检查规则是否已存在
        /// </summary>
        private static bool CheckIfRuleExists(object commonKnowledge)
        {
            try
            {
                // 调用 GetEntryById(id) 方法
                MethodInfo getEntryMethod = commonKnowledge.GetType().GetMethod("GetEntryById");
                if (getEntryMethod != null)
                {
                    object entry = getEntryMethod.Invoke(commonKnowledge, new object[] { RECRUIT_RULE_ID });
                    return entry != null;
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimTalk-ExpandActions] CheckIfRuleExists 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 创建招募规则条目
        /// </summary>
        private static object CreateRecruitRuleEntry(float importance, string customContent)
        {
            try
            {
                Type entryType = FindType($"{TARGET_MOD_NAMESPACE}.{KNOWLEDGE_ENTRY_TYPE}");
                if (entryType == null)
                {
                    Log.Error("[RimTalk-ExpandActions] 未找到 CommonKnowledgeEntry 类型");
                    return null;
                }

                // 创建实例
                object entry = Activator.CreateInstance(entryType);

                // 设置基本属性
                SetProperty(entry, "id", RECRUIT_RULE_ID);
                SetProperty(entry, "isEnabled", true);
                SetProperty(entry, "targetPawnId", -1);

                // 设置关键词
                var keywordsList = Activator.CreateInstance(typeof(System.Collections.Generic.List<string>));
                var addMethod = keywordsList.GetType().GetMethod("Add");
                string[] keywords = { "招募", "加入", "投靠", "派系", "跟我走", "收留", "收编", "归顺" };
                foreach (var keyword in keywords)
                {
                    addMethod.Invoke(keywordsList, new object[] { keyword });
                }
                SetProperty(entry, "keywords", keywordsList);

                // 格式化内容为 [tag|importance]content
                string ruleContent = string.IsNullOrEmpty(customContent) ? GetDefaultRecruitRuleContent() : customContent;
                string formattedContent = $"[{RECRUIT_RULE_TAG}|{importance:F1}]{ruleContent}";
                SetProperty(entry, "content", formattedContent);

                return entry;
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-ExpandActions] CreateRecruitRuleEntry 失败: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// 添加条目到常识库
        /// </summary>
        private static bool AddEntryToCommonKnowledge(object commonKnowledge, object entry)
        {
            try
            {
                // ???entry?????
                Type entryType = entry.GetType();
                
                // ?????????????????????
                MethodInfo addMethod = commonKnowledge.GetType().GetMethod(
                    "AddEntry",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new Type[] { entryType },
                    null
                );
                
                if (addMethod == null)
                {
                    // ???AddEntry??????????
                    var allMethods = commonKnowledge.GetType().GetMethods()
                        .Where(m => m.Name == "AddEntry")
                        .ToArray();
                    
                    if (allMethods.Length > 0)
                    {
                        // ??????????????
                        addMethod = allMethods[0];
                        Log.Warning($"[RimTalk-ExpandActions] ??????AddEntry????: {addMethod.GetParameters()[0].ParameterType.Name}");
                    }
                }
                
                if (addMethod != null)
                {
                    addMethod.Invoke(commonKnowledge, new object[] { entry });
                    return true;
                }
                
                Log.Error("[RimTalk-ExpandActions] δ???AddEntry????");
                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-ExpandActions] AddEntryToCommonKnowledge ???: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 设置对象属性
        /// </summary>
        private static void SetProperty(object obj, string propertyName, object value)
        {
            try
            {
                PropertyInfo prop = obj.GetType().GetProperty(propertyName);
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(obj, value);
                }
                else
                {
                    // 尝试字段
                    FieldInfo field = obj.GetType().GetField(propertyName);
                    if (field != null)
                    {
                        field.SetValue(obj, value);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimTalk-ExpandActions] SetProperty {propertyName} 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取默认招募规则内容
        /// </summary>
        private static string GetDefaultRecruitRuleContent()
        {
            return @"当谈话涉及【招募、加入、投靠、派系】话题，且目标NPC在对话中明确表示同意加入玩家派系时（例如说""我愿意加入""、""好吧，我跟你走""），请务必在回复的最后附加如下JSON代码：{""action"": ""recruit"", ""target"": ""NPC名字""}。注意：仅在对方明确同意时才输出此指令，拒绝或犹豫时不输出。";
        }

        #endregion

        #region 通用规则注入方法

        /// <summary>
        /// 通用规则注入方法（支持任意规则）
        /// </summary>
        /// <param name="ruleId">规则 ID</param>
        /// <param name="tag">标签</param>
        /// <param name="content">内容</param>
        /// <param name="keywords">关键词数组</param>
        /// <param name="importance">重要性</param>
        /// <returns>是否注入成功</returns>
        public static bool TryInjectRule(string ruleId, string tag, string content, string[] keywords, float importance = 1.0f)
        {
            try
            {
                // 1. 查找 MemoryManager 类型
                Type memoryManagerType = FindType($"{TARGET_MOD_NAMESPACE}.{MEMORY_MANAGER_TYPE}");
                if (memoryManagerType == null)
                {
                    Log.Warning($"[RimTalk-ExpandActions] 未找到 RimTalk.Memory.MemoryManager 类型");
                    return false;
                }

                // 2. 获取 CommonKnowledge 实例
                object commonKnowledge = GetCommonKnowledgeInstance(memoryManagerType);
                if (commonKnowledge == null)
                {
                    Log.Error($"[RimTalk-ExpandActions] 无法获取 CommonKnowledge 实例");
                    return false;
                }

                // 3. 检查规则是否已存在
                if (CheckIfRuleExistsByRuleId(commonKnowledge, ruleId))
                {
                    Log.Message($"[RimTalk-ExpandActions] 规则 {ruleId} 已存在，跳过注入");
                    return true; // 已存在算成功
                }

                // 4. 创建新的规则条目
                object ruleEntry = CreateGenericRuleEntry(ruleId, tag, content, keywords, importance);
                if (ruleEntry == null)
                {
                    Log.Error($"[RimTalk-ExpandActions] 创建规则条目失败: {ruleId}");
                    return false;
                }

                // 5. 添加到常识库
                bool added = AddEntryToCommonKnowledge(commonKnowledge, ruleEntry);
                if (added)
                {
                    Log.Message($"[RimTalk-ExpandActions] ? 成功注入规则: {ruleId}");
                    return true;
                }
                else
                {
                    Log.Error($"[RimTalk-ExpandActions] 添加规则到常识库失败: {ruleId}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-ExpandActions] 注入规则失败 ({ruleId}): {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 检查指定 ID 的规则是否已存在
        /// </summary>
        private static bool CheckIfRuleExistsByRuleId(object commonKnowledge, string ruleId)
        {
            try
            {
                MethodInfo getEntryMethod = commonKnowledge.GetType().GetMethod("GetEntryById");
                if (getEntryMethod != null)
                {
                    object entry = getEntryMethod.Invoke(commonKnowledge, new object[] { ruleId });
                    return entry != null;
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimTalk-ExpandActions] CheckIfRuleExistsByRuleId 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 创建通用规则条目
        /// </summary>
        private static object CreateGenericRuleEntry(string ruleId, string tag, string content, string[] keywords, float importance)
        {
            try
            {
                Type entryType = FindType($"{TARGET_MOD_NAMESPACE}.{KNOWLEDGE_ENTRY_TYPE}");
                if (entryType == null)
                {
                    Log.Error("[RimTalk-ExpandActions] 未找到 CommonKnowledgeEntry 类型");
                    return null;
                }

                // 创建实例
                object entry = Activator.CreateInstance(entryType);

                // 设置基本属性
                SetProperty(entry, "id", ruleId);
                SetProperty(entry, "isEnabled", true);
                SetProperty(entry, "targetPawnId", -1);

                // 格式化内容为 [tag|importance]content
                string tagText = tag ?? "对话行为";
                string formattedContent = $"[{tagText}|{importance:F1}]{content}";
                SetProperty(entry, "content", formattedContent);

                // 设置关键词列表
                if (keywords != null && keywords.Length > 0)
                {
                    var keywordsList = Activator.CreateInstance(typeof(System.Collections.Generic.List<string>));
                    var addMethod = keywordsList.GetType().GetMethod("Add");
                    
                    foreach (var keyword in keywords)
                    {
                        if (!string.IsNullOrWhiteSpace(keyword))
                        {
                            addMethod.Invoke(keywordsList, new object[] { keyword });
                        }
                    }
                    
                    SetProperty(entry, "keywords", keywordsList);
                }

                return entry;
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-ExpandActions] CreateGenericRuleEntry 失败: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// 批量移除规则
        /// </summary>
        /// <param name="ruleIds">要移除的规则 ID 列表</param>
        /// <returns>成功移除的数量</returns>
        public static int RemoveRules(params string[] ruleIds)
        {
            try
            {
                Type memoryManagerType = FindType($"{TARGET_MOD_NAMESPACE}.{MEMORY_MANAGER_TYPE}");
                if (memoryManagerType == null) return 0;

                object commonKnowledge = GetCommonKnowledgeInstance(memoryManagerType);
                if (commonKnowledge == null) return 0;

                MethodInfo removeMethod = commonKnowledge.GetType().GetMethod("RemoveEntry");
                if (removeMethod == null) return 0;

                int removedCount = 0;
                foreach (var ruleId in ruleIds)
                {
                    try
                    {
                        removeMethod.Invoke(commonKnowledge, new object[] { ruleId });
                        removedCount++;
                        Log.Message($"[RimTalk-ExpandActions] 已移除规则: {ruleId}");
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[RimTalk-ExpandActions] 移除规则失败 ({ruleId}): {ex.Message}");
                    }
                }

                if (removedCount > 0)
                {
                    Messages.Message($"已移除 {removedCount} 条规则", MessageTypeDefOf.NeutralEvent);
                }

                return removedCount;
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-ExpandActions] RemoveRules 失败: {ex.Message}");
                return 0;
            }
        }

        #endregion
    }
}
