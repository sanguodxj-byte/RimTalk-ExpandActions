using System;
using System.Linq;
using System.Text.RegularExpressions;
using RimWorld;
using Verse;
using RimTalkExpandActions.Memory.Actions;

namespace RimTalkExpandActions.Memory
{
    /// <summary>
    /// AI 回复后处理器，用于解析和执行嵌入在回复文本中的动作指令
    /// </summary>
    public static class AIResponsePostProcessor
    {
        // 基础 JSON 匹配正则表达式
        private static readonly Regex BaseJsonRegex = new Regex(
            @"\{[^}]*""action""\s*:\s*""([^""]+)""[^}]*\}",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        /// <summary>
        /// 处理 AI 回复文本，检测并执行动作指令
        /// </summary>
        public static string ProcessActionResponse(string responseText, Pawn targetPawn, Pawn recruiter = null)
        {
            if (string.IsNullOrEmpty(responseText))
            {
                return responseText;
            }

            try
            {
                // 1. 检测是否包含动作 JSON
                if (!responseText.Contains("{\"action\"") && !responseText.Contains("{ \"action\""))
                {
                    return responseText;
                }

                // 2. 使用正则表达式匹配 JSON
                Match match = BaseJsonRegex.Match(responseText);
                
                if (match.Success)
                {
                    string jsonBlock = match.Value;
                    string action = ExtractJsonField(jsonBlock, "action");
                    if (action != null)
                    {
                        action = action.ToLower();
                    }

                    if (!string.IsNullOrEmpty(action))
                    {
                        // 3. 根据动作类型分发到对应处理器
                        DispatchAction(action, jsonBlock, targetPawn, recruiter);
                    }

                    // 4. 移除 JSON 字符串，返回纯净文本
                    string cleanText = responseText.Replace(jsonBlock, "").Trim();
                    return cleanText;
                }

                return responseText;
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("[RimTalk-ExpandActions] ProcessActionResponse 处理失败: {0}\n{1}", ex.Message, ex.StackTrace));
                return responseText;
            }
        }

        /// <summary>
        /// 分发动作到对应的处理器
        /// </summary>
        private static void DispatchAction(string action, string jsonBlock, Pawn targetPawn, Pawn recruiter)
        {
            try
            {
                // 获取设置
                var settings = RimTalkExpandActionsMod.Settings;

                // 检查行为是否启用
                if (settings != null && !settings.IsActionEnabled(action))
                {
                    if (settings.enableDetailedLogging)
                    {
                        Log.Message(string.Format("[RimTalk-ExpandActions] 行为 '{0}' 已禁用，跳过执行", action));
                    }
                    return;
                }

                // 检查成功率
                if (settings != null)
                {
                    float successChance = settings.GetSuccessChance(action);
                    float roll = Rand.Value;

                    if (roll > successChance)
                    {
                        if (settings.enableDetailedLogging)
                        {
                            Log.Message(string.Format("[RimTalk-ExpandActions] 行为 '{0}' 成功率检定失败 ({1:F2} > {2:F2})", action, roll, successChance));
                        }

                        if (settings.showActionMessages)
                        {
                            Messages.Message(
                                string.Format("{0} 的 {1} 尝试失败了...", targetPawn.Name.ToStringShort, GetActionDisplayName(action)),
                                targetPawn,
                                MessageTypeDefOf.RejectInput
                            );
                        }
                        return;
                    }

                    if (settings.enableDetailedLogging)
                    {
                        Log.Message(string.Format("[RimTalk-ExpandActions] 行为 '{0}' 成功率检定通过 ({1:F2} <= {2:F2})", action, roll, successChance));
                    }
                }

                // 执行对应的动作
                switch (action)
                {
                    case "recruit":
                        HandleRecruitAction(jsonBlock, targetPawn, recruiter);
                        break;

                    case "drop_weapon":
                        HandleDropWeaponAction(jsonBlock, targetPawn);
                        break;

                    case "romance":
                        HandleRomanceAction(jsonBlock, targetPawn);
                        break;

                    case "give_inspiration":
                        HandleInspirationAction(jsonBlock, targetPawn);
                        break;

                    case "force_rest":
                        HandleRestAction(jsonBlock, targetPawn);
                        break;

                    case "give_item":
                        HandleGiftAction(jsonBlock, targetPawn);
                        break;

                    case "social_dining":
                        HandleSocialDiningAction(jsonBlock, targetPawn, recruiter);
                        break;

                    default:
                        Log.Warning(string.Format("[RimTalk-ExpandActions] 未知动作类型: {0}", action));
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("[RimTalk-ExpandActions] DispatchAction 失败 ({0}): {1}\n{2}", action, ex.Message, ex.StackTrace));
            }
        }

        /// <summary>
        /// 获取动作的显示名称
        /// </summary>
        private static string GetActionDisplayName(string action)
        {
            if (action == null) return "";
            
            switch (action.ToLower())
            {
                case "recruit":
                    return "招募";
                case "drop_weapon":
                    return "投降";
                case "romance":
                    return "恋爱";
                case "give_inspiration":
                    return "灵感";
                case "force_rest":
                    return "休息";
                case "give_item":
                    return "送礼";
                case "social_dining":
                    return "共餐";
                default:
                    return action;
            }
        }

        #region 动作处理器

        private static void HandleRecruitAction(string jsonBlock, Pawn targetPawn, Pawn recruiter)
        {
            string targetName = ExtractJsonField(jsonBlock, "target");
            
            if (ValidateTarget(targetName, targetPawn))
            {
                Log.Message(string.Format("[RimTalk-ExpandActions] 检测到招募指令: {0}", targetPawn.Name.ToStringShort));
                RimTalkActions.ExecuteRecruit(targetPawn, recruiter);
            }
        }

        private static void HandleDropWeaponAction(string jsonBlock, Pawn targetPawn)
        {
            string targetName = ExtractJsonField(jsonBlock, "target");
            
            if (ValidateTarget(targetName, targetPawn))
            {
                Log.Message(string.Format("[RimTalk-ExpandActions] 检测到投降指令: {0}", targetPawn.Name.ToStringShort));
                RimTalkActions.ExecuteDropWeapon(targetPawn);
            }
        }

        private static void HandleRomanceAction(string jsonBlock, Pawn targetPawn)
        {
            string targetName = ExtractJsonField(jsonBlock, "target");
            string partnerName = ExtractJsonField(jsonBlock, "partner");
            string type = ExtractJsonField(jsonBlock, "type");

            if (!ValidateTarget(targetName, targetPawn))
            {
                return;
            }

            Pawn partner = FindPawnByName(partnerName);
            if (partner == null)
            {
                Log.Warning(string.Format("[RimTalk-ExpandActions] 未找到名为 '{0}' 的 Pawn", partnerName));
                return;
            }

            Log.Message(string.Format("[RimTalk-ExpandActions] 检测到恋爱指令: {0} <-> {1}, 类型: {2}", targetPawn.Name.ToStringShort, partner.Name.ToStringShort, type));
            RimTalkActions.ExecuteRomanceChange(targetPawn, partner, type);
        }

        private static void HandleInspirationAction(string jsonBlock, Pawn targetPawn)
        {
            string targetName = ExtractJsonField(jsonBlock, "target");
            string type = ExtractJsonField(jsonBlock, "type");

            if (ValidateTarget(targetName, targetPawn))
            {
                Log.Message(string.Format("[RimTalk-ExpandActions] 检测到灵感指令: {0}, 类型: {1}", targetPawn.Name.ToStringShort, type));
                RimTalkActions.ExecuteInspiration(targetPawn, type);
            }
        }

        private static void HandleRestAction(string jsonBlock, Pawn targetPawn)
        {
            string targetName = ExtractJsonField(jsonBlock, "target");
            string immediateStr = ExtractJsonField(jsonBlock, "immediate");
            bool immediate = immediateStr != null && immediateStr.ToLower() == "true";

            if (ValidateTarget(targetName, targetPawn))
            {
                Log.Message(string.Format("[RimTalk-ExpandActions] 检测到休息指令: {0}, 立即: {1}", targetPawn.Name.ToStringShort, immediate));
                RimTalkActions.ExecuteRest(targetPawn, immediate);
            }
        }

        private static void HandleGiftAction(string jsonBlock, Pawn targetPawn)
        {
            string targetName = ExtractJsonField(jsonBlock, "target");
            string itemKeyword = ExtractJsonField(jsonBlock, "item_keyword");

            if (ValidateTarget(targetName, targetPawn) && !string.IsNullOrEmpty(itemKeyword))
            {
                Log.Message(string.Format("[RimTalk-ExpandActions] 检测到送礼指令: {0}, 物品: {1}", targetPawn.Name.ToStringShort, itemKeyword));
                RimTalkActions.ExecuteGift(targetPawn, itemKeyword);
            }
        }

        private static void HandleSocialDiningAction(string jsonBlock, Pawn targetPawn, Pawn recruiter)
        {
            // 尝试从 JSON 获取目标
            string targetName = ExtractJsonField(jsonBlock, "target");
            Pawn finalTarget = null;
            
            if (!string.IsNullOrEmpty(targetName))
            {
                finalTarget = FindPawnByName(targetName);
                if (finalTarget == null)
                {
                    Log.Warning(string.Format("[RimTalk-ExpandActions] 未找到目标: '{0}'，使用默认 targetPawn", targetName));
                }
            }
            
            // 如果 JSON 没有指定目标或解析失败，使用参数 targetPawn
            if (finalTarget == null)
            {
                finalTarget = targetPawn;
            }

            // 尝试从 JSON 获取发起者
            string initiatorName = ExtractJsonField(jsonBlock, "initiator");
            Pawn finalInitiator = null;
            
            if (!string.IsNullOrEmpty(initiatorName))
            {
                finalInitiator = FindPawnByName(initiatorName);
                if (finalInitiator == null)
                {
                    Log.Warning(string.Format("[RimTalk-ExpandActions] 未找到发起者: '{0}'，使用默认 recruiter", initiatorName));
                }
            }
            
            // 如果 JSON 没有指定发起者或解析失败，使用参数 recruiter（当前说话者）
            if (finalInitiator == null)
            {
                finalInitiator = recruiter;
            }

            // 空值检查
            if (finalInitiator == null)
            {
                Log.Warning("[RimTalk-ExpandActions] 社交用餐指令：发起者为空，无法执行");
                return;
            }

            if (finalTarget == null)
            {
                Log.Warning("[RimTalk-ExpandActions] 社交用餐指令：目标为空，无法执行");
                return;
            }

            // 检查是否为同一个人
            if (finalInitiator == finalTarget)
            {
                Log.Warning(string.Format("[RimTalk-ExpandActions] 社交用餐指令：发起者和目标是同一个人 ({0})，无法执行", 
                    finalInitiator.Name.ToStringShort));
                return;
            }

            Log.Message(string.Format("[RimTalk-ExpandActions] 检测到社交用餐指令: {0} 邀请 {1}", 
                finalInitiator.Name.ToStringShort, finalTarget.Name.ToStringShort));
            
            RimTalkActions.ExecuteSocialDining(finalInitiator, finalTarget);
        }

        #endregion

        #region 辅助方法

        private static string ExtractJsonField(string jsonBlock, string fieldName)
        {
            try
            {
                // 使用正则表达式提取字段值
                string pattern = string.Format(@"""{0}""\s*:\s*""([^""]+)""", fieldName);
                Match match = Regex.Match(jsonBlock, pattern, RegexOptions.IgnoreCase);
                
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }

                // 尝试匹配布尔值或数字（不带引号）
                pattern = string.Format(@"""{0}""\s*:\s*(\w+)", fieldName);
                match = Regex.Match(jsonBlock, pattern, RegexOptions.IgnoreCase);
                
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }

                return null;
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("[RimTalk-ExpandActions] ExtractJsonField 失败 ({0}): {1}", fieldName, ex.Message));
                return null;
            }
        }

        private static bool ValidateTarget(string targetName, Pawn targetPawn)
        {
            try
            {
                if (targetPawn == null || string.IsNullOrEmpty(targetName))
                {
                    return false;
                }

                string shortName = targetPawn.Name != null ? targetPawn.Name.ToStringShort : "";
                string fullName = targetPawn.Name != null ? targetPawn.Name.ToStringFull : "";
                string nickname = "";
                NameTriple nameTriple = targetPawn.Name as NameTriple;
                if (nameTriple != null)
                {
                    nickname = nameTriple.Nick ?? "";
                }

                string normalizedTarget = targetName.ToLower().Replace(" ", "");
                string normalizedShort = shortName.ToLower().Replace(" ", "");
                string normalizedFull = fullName.ToLower().Replace(" ", "");
                string normalizedNick = nickname.ToLower().Replace(" ", "");

                bool isMatch = normalizedTarget.Contains(normalizedShort) ||
                              normalizedTarget.Contains(normalizedNick) ||
                              normalizedShort.Contains(normalizedTarget) ||
                              normalizedNick.Contains(normalizedTarget) ||
                              normalizedFull.Contains(normalizedTarget);

                if (!isMatch)
                {
                    Log.Warning(string.Format("[RimTalk-ExpandActions] 名字不匹配 - JSON目标: '{0}', 实际: '{1}' / '{2}'", targetName, shortName, nickname));
                }

                return isMatch;
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("[RimTalk-ExpandActions] ValidateTarget 验证失败: {0}", ex.Message));
                return false;
            }
        }

        private static Pawn FindPawnByName(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return null;
                }

                string normalizedName = name.ToLower().Replace(" ", "");

                foreach (var map in Find.Maps)
                {
                    foreach (var pawn in map.mapPawns.AllPawns)
                    {
                        if (pawn == null || pawn.Name == null) continue;

                        string shortName = pawn.Name.ToStringShort != null ? pawn.Name.ToStringShort.ToLower().Replace(" ", "") : "";
                        string nickname = "";
                        NameTriple nameTriple = pawn.Name as NameTriple;
                        if (nameTriple != null && nameTriple.Nick != null)
                        {
                            nickname = nameTriple.Nick.ToLower().Replace(" ", "");
                        }

                        if (shortName.Contains(normalizedName) || 
                            normalizedName.Contains(shortName) ||
                            nickname.Contains(normalizedName) ||
                            normalizedName.Contains(nickname))
                        {
                            return pawn;
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("[RimTalk-ExpandActions] FindPawnByName 失败: {0}", ex.Message));
                return null;
            }
        }

        public static bool TryParseActionJson(string json, out string action, out string target)
        {
            action = null;
            target = null;

            try
            {
                var match = BaseJsonRegex.Match(json);
                if (match.Success)
                {
                    action = ExtractJsonField(match.Value, "action");
                    target = ExtractJsonField(match.Value, "target");
                    return !string.IsNullOrEmpty(action);
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("[RimTalk-ExpandActions] TryParseActionJson 失败: {0}", ex.Message));
                return false;
            }
        }

        #endregion
    }
}
