using System;
using UnityEngine;
using Verse;
using RimWorld;
using RimTalkExpandActions.Memory.Utils;

namespace RimTalkExpandActions
{
    public static class RimTalkExpandActionsSettingsUI
    {
        private static Vector2 scrollPosition = Vector2.zero;
        private static string customContentBuffer = "";
        private static bool initialized = false;

        private const float ROW_HEIGHT = 30f;
        private const float LABEL_WIDTH = 250f;
        private const float GAP = 12f;
        private const float BUTTON_WIDTH = 200f;
        private const float BUTTON_HEIGHT = 35f;
        private const float SLIDER_WIDTH = 300f;

        // 行为开关数据结构
        private class BehaviorToggleData
        {
            public string Label;
            public string ActionType;
            
            public BehaviorToggleData(string label, string actionType)
            {
                Label = label;
                ActionType = actionType;
            }
        }

        public static void DoSettingsWindowContents(Rect inRect, RimTalkExpandActionsSettings settings)
        {
            if (!initialized)
            {
                customContentBuffer = settings.customRecruitRuleContent;
                initialized = true;
            }

            float contentHeight = CalculateContentHeight();
            Rect viewRect = new Rect(0f, 0f, inRect.width - 20f, contentHeight);

            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);
            
            float curY = 0f;

            curY = DrawHeader(viewRect.width, curY);
            curY += GAP * 2;

            curY = DrawRuleStatusSection(viewRect.width, curY, settings);
            curY += GAP * 2;

            curY = DrawGlobalSettings(viewRect.width, curY, settings);
            curY += GAP * 2;

            curY = DrawBehaviorToggles(viewRect.width, curY, settings);
            curY += GAP * 2;

            curY = DrawSuccessChanceSliders(viewRect.width, curY, settings);
            curY += GAP * 2;

            curY = DrawAdvancedSettings(viewRect.width, curY, settings);
            curY += GAP * 2;

            curY = DrawCustomRuleContent(viewRect.width, curY, settings);
            curY += GAP * 2;

            curY = DrawActionButtons(viewRect.width, curY, settings);

            Widgets.EndScrollView();
        }

        #region 绘制各个区域

        private static float DrawHeader(float width, float curY)
        {
            Rect titleRect = new Rect(0f, curY, width, 40f);
            Widgets.Label(titleRect, "RimTalk-ExpandActions 设置");
            curY += 40f;

            Rect descRect = new Rect(0f, curY, width, 60f);
            Widgets.Label(descRect,
                "本 Mod 提供 6 种对话触发的行为系统。您可以单独启用/禁用每种行为，\n" +
                "并调整成功难度系数（0.0 = 必定失败，1.0 = 100%成功）。");
            curY += 60f;

            return curY;
        }

        private static float DrawRuleStatusSection(float width, float curY, RimTalkExpandActionsSettings settings)
        {
            bool rimTalkExists = CheckIfRimTalkExists();
            if (!rimTalkExists)
            {
                Rect warningRect = new Rect(0f, curY, width, 50f);
                GUI.color = Color.yellow;
                Widgets.Label(warningRect,
                    "? 警告：未检测到 RimTalk-ExpandMemory Mod！\n" +
                    "请确保该 Mod 已安装并在加载顺序中位于本 Mod 之前。");
                GUI.color = Color.white;
                curY += 55f;
            }

            bool ruleExists = CrossModRecruitRuleInjector.CheckIfRecruitRuleExists();
            Rect statusRect = new Rect(0f, curY, width, ROW_HEIGHT);
            string statusText = ruleExists ? "? 系统规则已注入到常识库" : "? 系统规则尚未注入";
            GUI.color = ruleExists ? Color.green : Color.yellow;
            Widgets.Label(statusRect, statusText);
            GUI.color = Color.white;
            curY += ROW_HEIGHT + GAP;

            Rect buttonRect = new Rect(0f, curY, BUTTON_WIDTH, BUTTON_HEIGHT);
            if (!ruleExists)
            {
                if (Widgets.ButtonText(buttonRect, "立即注入所有规则"))
                {
                    HandleInjectButton(settings);
                }
            }
            else
            {
                Rect removeButtonRect = new Rect(BUTTON_WIDTH + 10f, curY, BUTTON_WIDTH, BUTTON_HEIGHT);
                
                if (Widgets.ButtonText(buttonRect, "重新注入规则"))
                {
                    CrossModRecruitRuleInjector.RemoveRules(
                        "sys-rule-recruit", "sys-rule-drop-weapon", "sys-rule-romance",
                        "sys-rule-inspiration", "sys-rule-rest", "sys-rule-gift"
                    );
                    System.Threading.Thread.Sleep(100);
                    HandleInjectButton(settings);
                }

                GUI.color = new Color(1f, 0.7f, 0.7f);
                if (Widgets.ButtonText(removeButtonRect, "移除所有规则"))
                {
                    int removed = CrossModRecruitRuleInjector.RemoveRules(
                        "sys-rule-recruit", "sys-rule-drop-weapon", "sys-rule-romance",
                        "sys-rule-inspiration", "sys-rule-rest", "sys-rule-gift"
                    );
                    Messages.Message(string.Format("已移除 {0} 条规则", removed), MessageTypeDefOf.NeutralEvent);
                }
                GUI.color = Color.white;
            }
            curY += BUTTON_HEIGHT;

            return curY;
        }

        private static float DrawGlobalSettings(float width, float curY, RimTalkExpandActionsSettings settings)
        {
            DrawSectionTitle("全局设置", width, ref curY);

            Rect autoInjectRect = new Rect(0f, curY, width, ROW_HEIGHT);
            Widgets.CheckboxLabeled(autoInjectRect, "游戏启动时自动注入规则", ref settings.autoInjectRules);
            curY += ROW_HEIGHT + GAP;

            Rect importanceLabelRect = new Rect(0f, curY, LABEL_WIDTH, ROW_HEIGHT);
            Rect importanceSliderRect = new Rect(LABEL_WIDTH, curY, SLIDER_WIDTH, ROW_HEIGHT);

            Widgets.Label(importanceLabelRect, string.Format("规则重要性（AI 检索优先级）: {0:F2}", settings.ruleImportance));
            settings.ruleImportance = Widgets.HorizontalSlider(
                importanceSliderRect,
                settings.ruleImportance,
                0.0f,
                1.0f,
                true,
                null,
                "0.0",
                "1.0"
            );
            curY += ROW_HEIGHT + GAP;

            return curY;
        }

        private static float DrawBehaviorToggles(float width, float curY, RimTalkExpandActionsSettings settings)
        {
            DrawSectionTitle("行为开关", width, ref curY);

            var behaviors = new BehaviorToggleData[]
            {
                new BehaviorToggleData("启用招募功能", "recruit"),
                new BehaviorToggleData("启用投降/丢武器功能", "drop_weapon"),
                new BehaviorToggleData("启用恋爱关系功能", "romance"),
                new BehaviorToggleData("启用灵感触发功能", "inspiration"),
                new BehaviorToggleData("启用休息/昏迷功能", "rest"),
                new BehaviorToggleData("启用物品赠送功能", "gift")
            };

            foreach (var behavior in behaviors)
            {
                Rect toggleRect = new Rect(0f, curY, width, ROW_HEIGHT);
                bool currentValue = GetBehaviorToggleValue(settings, behavior.ActionType);
                bool newValue = currentValue;
                Widgets.CheckboxLabeled(toggleRect, behavior.Label, ref newValue);
                
                if (newValue != currentValue)
                {
                    UpdateBehaviorToggle(settings, behavior.ActionType, newValue);
                }
                
                curY += ROW_HEIGHT + 6f;
            }

            return curY + GAP;
        }

        private static float DrawSuccessChanceSliders(float width, float curY, RimTalkExpandActionsSettings settings)
        {
            DrawSectionTitle("成功难度系数（0.0 = 必定失败，1.0 = 100%成功）", width, ref curY);

            DrawChanceSlider("招募成功率", ref settings.recruitSuccessChance, width, ref curY);
            DrawChanceSlider("投降成功率", ref settings.dropWeaponSuccessChance, width, ref curY);
            DrawChanceSlider("恋爱成功率", ref settings.romanceSuccessChance, width, ref curY);
            DrawChanceSlider("灵感触发成功率", ref settings.inspirationSuccessChance, width, ref curY);
            DrawChanceSlider("休息成功率", ref settings.restSuccessChance, width, ref curY);
            DrawChanceSlider("赠送成功率", ref settings.giftSuccessChance, width, ref curY);

            return curY;
        }

        private static float DrawAdvancedSettings(float width, float curY, RimTalkExpandActionsSettings settings)
        {
            DrawSectionTitle("高级设置", width, ref curY);

            Rect logRect = new Rect(0f, curY, width, ROW_HEIGHT);
            Widgets.CheckboxLabeled(logRect, "启用详细日志（用于调试）", ref settings.enableDetailedLogging);
            curY += ROW_HEIGHT + 6f;

            Rect msgRect = new Rect(0f, curY, width, ROW_HEIGHT);
            Widgets.CheckboxLabeled(msgRect, "显示行为触发提示消息", ref settings.showActionMessages);
            curY += ROW_HEIGHT + GAP;

            return curY;
        }

        private static float DrawCustomRuleContent(float width, float curY, RimTalkExpandActionsSettings settings)
        {
            DrawSectionTitle("自定义招募规则内容（留空使用默认）", width, ref curY);

            Rect textAreaRect = new Rect(0f, curY, width, 120f);
            customContentBuffer = Widgets.TextArea(textAreaRect, customContentBuffer);
            settings.customRecruitRuleContent = customContentBuffer;
            curY += 125f;

            return curY;
        }

        private static float DrawActionButtons(float width, float curY, RimTalkExpandActionsSettings settings)
        {
            Rect resetButtonRect = new Rect(0f, curY, BUTTON_WIDTH * 0.8f, ROW_HEIGHT);
            if (Widgets.ButtonText(resetButtonRect, "重置为默认"))
            {
                settings.ResetToDefault();
                customContentBuffer = "";
                Messages.Message("设置已重置为默认值", MessageTypeDefOf.NeutralEvent);
            }
            curY += ROW_HEIGHT + GAP * 2;

            Rect helpRect = new Rect(0f, curY, width, 80f);
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            Widgets.Label(helpRect,
                "【使用说明】\n" +
                "1. 启用您需要的行为功能\n" +
                "2. 调整成功率系数控制触发难度\n" +
                "3. 点击\"立即注入所有规则\"将规则添加到 RimTalk 常识库\n" +
                "4. AI 在对话时会根据设置自动处理");
            GUI.color = Color.white;
            curY += 80f;

            return curY;
        }

        #endregion

        #region 辅助方法

        private static void DrawSectionTitle(string title, float width, ref float curY)
        {
            
            Rect titleRect = new Rect(0f, curY, width, 30f);
            Widgets.Label(titleRect, title);
            
            curY += 35f;
        }

        private static void DrawChanceSlider(string label, ref float value, float width, ref float curY)
        {
            Rect labelRect = new Rect(0f, curY, LABEL_WIDTH, ROW_HEIGHT);
            Rect sliderRect = new Rect(LABEL_WIDTH, curY, SLIDER_WIDTH, ROW_HEIGHT);
            Rect percentRect = new Rect(LABEL_WIDTH + SLIDER_WIDTH + 10f, curY, 80f, ROW_HEIGHT);

            Widgets.Label(labelRect, string.Format("{0}:", label));
            value = Widgets.HorizontalSlider(sliderRect, value, 0.0f, 1.0f, true, null, "0%", "100%");
            Widgets.Label(percentRect, string.Format("{0:F0}%", value * 100f));

            curY += ROW_HEIGHT + 6f;
        }

        private static bool GetBehaviorToggleValue(RimTalkExpandActionsSettings settings, string actionType)
        {
            switch (actionType)
            {
                case "recruit":
                    return settings.enableRecruit;
                case "drop_weapon":
                    return settings.enableDropWeapon;
                case "romance":
                    return settings.enableRomance;
                case "inspiration":
                    return settings.enableInspiration;
                case "rest":
                    return settings.enableRest;
                case "gift":
                    return settings.enableGift;
                default:
                    return true;
            }
        }

        private static void UpdateBehaviorToggle(RimTalkExpandActionsSettings settings, string actionType, bool value)
        {
            switch (actionType)
            {
                case "recruit":
                    settings.enableRecruit = value;
                    break;
                case "drop_weapon":
                    settings.enableDropWeapon = value;
                    break;
                case "romance":
                    settings.enableRomance = value;
                    break;
                case "inspiration":
                    settings.enableInspiration = value;
                    break;
                case "rest":
                    settings.enableRest = value;
                    break;
                case "gift":
                    settings.enableGift = value;
                    break;
            }
        }

        private static float CalculateContentHeight()
        {
            return 1200f;
        }

        private static void HandleInjectButton(RimTalkExpandActionsSettings settings)
        {
            try
            {
                long currentTime = DateTime.UtcNow.Ticks;
                if (currentTime - settings.lastManualInjectTime < TimeSpan.FromSeconds(2).Ticks)
                {
                    Messages.Message("请勿频繁点击注入按钮", MessageTypeDefOf.RejectInput);
                    return;
                }
                settings.lastManualInjectTime = currentTime;

                var allRules = BehaviorRuleContents.GetAllRules();
                int successCount = 0;

                foreach (var ruleKvp in allRules)
                {
                    string ruleId = ruleKvp.Key;
                    RuleDefinition ruleDef = ruleKvp.Value;

                    string content = null;
                    if (ruleId == "sys-rule-recruit" && !string.IsNullOrWhiteSpace(settings.customRecruitRuleContent))
                    {
                        content = settings.customRecruitRuleContent;
                    }
                    else
                    {
                        content = ruleDef.Content;
                    }

                    if (CrossModRecruitRuleInjector.TryInjectRule(
                        ruleDef.Id, ruleDef.Tag, content, ruleDef.Keywords, ruleDef.Importance))
                    {
                        successCount++;
                    }
                }

                Messages.Message(string.Format("成功注入 {0} 条规则到常识库！", successCount), MessageTypeDefOf.PositiveEvent);
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("[RimTalk-ExpandActions] HandleInjectButton 失败: {0}\n{1}", ex.Message, ex.StackTrace));
                Messages.Message("注入失败，请查看日志", MessageTypeDefOf.RejectInput);
            }
        }

        private static bool CheckIfRimTalkExists()
        {
            try
            {
                foreach (var mod in LoadedModManager.RunningMods)
                {
                    if (mod.PackageId.ToLower().Contains("rimtalk") &&
                        mod.PackageId.ToLower().Contains("expandmemory"))
                    {
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
