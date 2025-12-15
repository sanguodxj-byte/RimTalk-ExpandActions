using System;
using UnityEngine;
using Verse;
using RimWorld;
using HarmonyLib;

namespace RimTalkExpandActions
{
    /// <summary>
    /// RimTalk-ExpandActions Mod 主类
    /// </summary>
    public class RimTalkExpandActionsMod : Mod
    {
        public static RimTalkExpandActionsSettings Settings { get; private set; }
        private static Vector2 scrollPosition;
        private static bool showBehaviorSettings = false;
        private static bool showSuccessRateSettings = false;

        public RimTalkExpandActionsMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<RimTalkExpandActionsSettings>();
            
            // 初始化 Harmony 补丁
            try
            {
                var harmony = new Harmony("sanguo.rimtalk.expandactions");
                harmony.PatchAll();
                Log.Message("[RimTalk-ExpandActions] Harmony 补丁已应用");
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-ExpandActions] Harmony 补丁失败: {ex.Message}\n{ex.StackTrace}");
            }
            
            Log.Message("[RimTalk-ExpandActions] Mod 已加载");
        }

        public override string SettingsCategory()
        {
            return "RimTalk-ExpandActions";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            try
            {
                // 创建滚动视图
                Rect viewRect = new Rect(0f, 0f, inRect.width - 20f, 1200f);
                Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);
                
                Listing_Standard listingStandard = new Listing_Standard();
                listingStandard.Begin(viewRect);
                
                // === 标题 ===
                Text.Font = GameFont.Medium;
                listingStandard.Label("RimTalk-ExpandActions v1.1.0");
                Text.Font = GameFont.Small;
                listingStandard.Gap();
                
                // === 全局设置 ===
                listingStandard.Label("━━━ 全局设置 ━━━");
                listingStandard.Gap(6f);
                
                listingStandard.CheckboxLabeled("显示行为触发消息", ref Settings.showActionMessages,
                    "在游戏中显示行为触发的提示消息");
                
                listingStandard.CheckboxLabeled("启用详细日志", ref Settings.enableDetailedLogging,
                    "在日志中记录详细的执行过程（用于调试）");
                
                listingStandard.Gap();
                
                // === 常识库注入 ===
                listingStandard.Label("━━━ 常识库管理 ━━━");
                listingStandard.Gap(6f);
                
                // 显示状态
                string status = Memory.Utils.ExpandMemoryKnowledgeInjector.CheckStatus();
                GUI.color = status.Contains("?") ? Color.green : Color.yellow;
                listingStandard.Label($"状态: {status}");
                GUI.color = Color.white;
                listingStandard.Gap(4f);
                
                // 注入按钮
                if (listingStandard.ButtonText("注入规则到当前存档", "将 7 种行为规则注入到当前存档的常识库"))
                {
                    InjectKnowledgeManually();
                }
                
                if (listingStandard.ButtonText("查看规则列表", "查看将要注入的 7 种行为规则"))
                {
                    ShowRuleList();
                }
                
                listingStandard.Gap(4f);
                
                // 帮助提示
                GUI.color = new Color(1f, 1f, 0.7f);
                listingStandard.Label("提示：常识库是存档级别的数据");
                listingStandard.Label("? 每次加载新存档后需要重新注入");
                listingStandard.Label("? 注入后规则将永久保存在该存档中");
                GUI.color = Color.white;
                
                listingStandard.Gap();
                
                // === 行为设置（可折叠） ===
                Rect behaviorHeaderRect = listingStandard.GetRect(30f);
                if (Widgets.ButtonText(behaviorHeaderRect, showBehaviorSettings ? "▼ 行为开关" : "? 行为开关"))
                {
                    showBehaviorSettings = !showBehaviorSettings;
                }
                
                if (showBehaviorSettings)
                {
                    listingStandard.Gap(6f);
                    listingStandard.Indent(20f);
                    
                    listingStandard.CheckboxLabeled("招募系统", ref Settings.enableRecruit,
                        "通过对话招募 NPC 到殖民地");
                    
                    listingStandard.CheckboxLabeled("社交用餐", ref Settings.enableSocialDining,
                        "邀请他人共进晚餐，增进关系");
                    
                    listingStandard.CheckboxLabeled("投降/丢武器", ref Settings.enableDropWeapon,
                        "让敌人放下武器投降");
                    
                    listingStandard.CheckboxLabeled("恋爱关系", ref Settings.enableRomance,
                        "建立或结束恋人关系");
                    
                    listingStandard.CheckboxLabeled("灵感触发", ref Settings.enableInspiration,
                        "给予角色工作/战斗/交易灵感");
                    
                    listingStandard.CheckboxLabeled("强制休息", ref Settings.enableRest,
                        "让角色去休息或陷入昏迷");
                    
                    listingStandard.CheckboxLabeled("赠送物品", ref Settings.enableGift,
                        "从背包中赠送物品给他人");
                    
                    listingStandard.CheckboxLabeled("社交放松", ref Settings.enableSocialRelax,
                        "指令多个小人进行社交放松活动");
                    
                    listingStandard.Outdent(20f);
                }
                
                listingStandard.Gap();
                
                // === 成功率设置（可折叠） ===
                Rect successRateHeaderRect = listingStandard.GetRect(30f);
                if (Widgets.ButtonText(successRateHeaderRect, showSuccessRateSettings ? "▼ 成功率设置" : "? 成功率设置"))
                {
                    showSuccessRateSettings = !showSuccessRateSettings;
                }
                
                if (showSuccessRateSettings)
                {
                    listingStandard.Gap(6f);
                    listingStandard.Indent(20f);
                    
                    listingStandard.Label($"招募成功率: {Settings.recruitSuccessChance:P0}");
                    Settings.recruitSuccessChance = listingStandard.Slider(Settings.recruitSuccessChance, 0f, 1f);
                    
                    listingStandard.Label($"社交用餐成功率: {Settings.socialDiningSuccessChance:P0}");
                    Settings.socialDiningSuccessChance = listingStandard.Slider(Settings.socialDiningSuccessChance, 0f, 1f);
                    
                    listingStandard.Label($"投降成功率: {Settings.dropWeaponSuccessChance:P0}");
                    Settings.dropWeaponSuccessChance = listingStandard.Slider(Settings.dropWeaponSuccessChance, 0f, 1f);
                    
                    listingStandard.Label($"恋爱成功率: {Settings.romanceSuccessChance:P0}");
                    Settings.romanceSuccessChance = listingStandard.Slider(Settings.romanceSuccessChance, 0f, 1f);
                    
                    listingStandard.Label($"灵感成功率: {Settings.inspirationSuccessChance:P0}");
                    Settings.inspirationSuccessChance = listingStandard.Slider(Settings.inspirationSuccessChance, 0f, 1f);
                    
                    listingStandard.Label($"休息成功率: {Settings.restSuccessChance:P0}");
                    Settings.restSuccessChance = listingStandard.Slider(Settings.restSuccessChance, 0f, 1f);
                    
                    listingStandard.Label($"赠送成功率: {Settings.giftSuccessChance:P0}");
                    Settings.giftSuccessChance = listingStandard.Slider(Settings.giftSuccessChance, 0f, 1f);
                    
                    listingStandard.Label($"社交放松成功率: {Settings.socialRelaxSuccessChance:P0}");
                    Settings.socialRelaxSuccessChance = listingStandard.Slider(Settings.socialRelaxSuccessChance, 0f, 1f);
                    
                    listingStandard.Outdent(20f);
                }
                
                listingStandard.Gap();
                
                // === 其他操作 ===
                listingStandard.Label("━━━ 其他操作 ━━━");
                listingStandard.Gap(6f);
                
                if (listingStandard.ButtonText("重置为默认值"))
                {
                    Settings.ResetToDefault();
                    Messages.Message("设置已重置为默认值", MessageTypeDefOf.NeutralEvent);
                }
                
                listingStandard.Gap();
                GUI.color = Color.gray;
                listingStandard.Label("提示：修改设置后会自动保存");
                GUI.color = Color.white;
                
                listingStandard.End();
                Widgets.EndScrollView();
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-ExpandActions] 设置界面错误: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void InjectKnowledgeManually()
        {
            try
            {
                var result = Memory.Utils.ExpandMemoryKnowledgeInjector.ManualInject();
                
                if (result.Success)
                {
                    string detailedMessage = $"成功导入 {result.InjectedRules} 条规则：\n\n";
                    var rules = Memory.Utils.ExpandMemoryKnowledgeInjector.GetRuleDescriptions();
                    
                    foreach (var ruleName in result.InjectedRuleNames)
                    {
                        if (rules.ContainsKey(ruleName))
                        {
                            detailedMessage += $"? {ruleName}\n  {rules[ruleName]}\n\n";
                        }
                    }
                    
                    Messages.Message(
                        $"RimTalk-ExpandActions: 已成功导入 {result.InjectedRules} 条行为规则到当前存档！",
                        MessageTypeDefOf.PositiveEvent,
                        false
                    );
                    
                    Log.Message($"[RimTalk-ExpandActions] 用户手动注入成功");
                    Log.Message(detailedMessage);
                }
                else
                {
                    Messages.Message(
                        $"注入失败: {result.ErrorMessage}",
                        MessageTypeDefOf.RejectInput,
                        false
                    );
                    Log.Warning($"[RimTalk-ExpandActions] 用户手动注入失败: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-ExpandActions] 手动注入异常: {ex.Message}");
                Messages.Message("注入失败，请查看日志", MessageTypeDefOf.RejectInput);
            }
        }

        private void ShowRuleList()
        {
            var rules = Memory.Utils.ExpandMemoryKnowledgeInjector.GetRuleDescriptions();
            string rulesList = "将注入以下 7 种行为规则：\n\n";
            
            int index = 1;
            foreach (var rule in rules)
            {
                rulesList += $"{index}. {rule.Key}\n   {rule.Value}\n\n";
                index++;
            }
            
            Find.WindowStack.Add(new Dialog_MessageBox(rulesList));
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            Log.Message("[RimTalk-ExpandActions] 设置已保存");
        }
    }

    /// <summary>
    /// RimTalk-ExpandActions Mod 设置数据
    /// </summary>
    public class RimTalkExpandActionsSettings : ModSettings
    {
        // ===== 全局设置 =====
        
        /// <summary>
        /// 是否在游戏中显示行为触发提示
        /// </summary>
        public bool showActionMessages = true;

        /// <summary>
        /// 是否显示详细日志
        /// </summary>
        public bool enableDetailedLogging = false;

        // ===== 行为开关 =====

        public bool enableRecruit = true;
        public bool enableDropWeapon = true;
        public bool enableRomance = true;
        public bool enableInspiration = true;
        public bool enableRest = true;
        public bool enableGift = true;
        public bool enableSocialDining = true;
        public bool enableSocialRelax = true;  // 新增：社交放松

        // ===== 成功难度系数 (0.0 - 1.0) =====

        public float recruitSuccessChance = 1.0f;
        public float dropWeaponSuccessChance = 1.0f;
        public float romanceSuccessChance = 1.0f;
        public float inspirationSuccessChance = 1.0f;
        public float restSuccessChance = 1.0f;
        public float giftSuccessChance = 1.0f;
        public float socialDiningSuccessChance = 1.0f;
        public float socialRelaxSuccessChance = 1.0f;  // 新增：社交放松成功率

        public override void ExposeData()
        {
            base.ExposeData();
            
            // 全局设置
            Scribe_Values.Look(ref showActionMessages, "showActionMessages", true);
            Scribe_Values.Look(ref enableDetailedLogging, "enableDetailedLogging", false);

            // 行为开关
            Scribe_Values.Look(ref enableRecruit, "enableRecruit", true);
            Scribe_Values.Look(ref enableDropWeapon, "enableDropWeapon", true);
            Scribe_Values.Look(ref enableRomance, "enableRomance", true);
            Scribe_Values.Look(ref enableInspiration, "enableInspiration", true);
            Scribe_Values.Look(ref enableRest, "enableRest", true);
            Scribe_Values.Look(ref enableGift, "enableGift", true);
            Scribe_Values.Look(ref enableSocialDining, "enableSocialDining", true);
            Scribe_Values.Look(ref enableSocialRelax, "enableSocialRelax", true);

            // 成功难度系数
            Scribe_Values.Look(ref recruitSuccessChance, "recruitSuccessChance", 1.0f);
            Scribe_Values.Look(ref dropWeaponSuccessChance, "dropWeaponSuccessChance", 1.0f);
            Scribe_Values.Look(ref romanceSuccessChance, "romanceSuccessChance", 1.0f);
            Scribe_Values.Look(ref inspirationSuccessChance, "inspirationSuccessChance", 1.0f);
            Scribe_Values.Look(ref restSuccessChance, "restSuccessChance", 1.0f);
            Scribe_Values.Look(ref giftSuccessChance, "giftSuccessChance", 1.0f);
            Scribe_Values.Look(ref socialDiningSuccessChance, "socialDiningSuccessChance", 1.0f);
            Scribe_Values.Look(ref socialRelaxSuccessChance, "socialRelaxSuccessChance", 1.0f);
        }

        /// <summary>
        /// 重置为默认值
        /// </summary>
        public void ResetToDefault()
        {
            showActionMessages = true;
            enableDetailedLogging = false;

            enableRecruit = true;
            enableDropWeapon = true;
            enableRomance = true;
            enableInspiration = true;
            enableRest = true;
            enableGift = true;
            enableSocialDining = true;
            enableSocialRelax = true;

            recruitSuccessChance = 1.0f;
            dropWeaponSuccessChance = 1.0f;
            romanceSuccessChance = 1.0f;
            inspirationSuccessChance = 1.0f;
            restSuccessChance = 1.0f;
            giftSuccessChance = 1.0f;
            socialDiningSuccessChance = 1.0f;
            socialRelaxSuccessChance = 1.0f;
        }

        /// <summary>
        /// 检查指定行为是否启用
        /// </summary>
        public bool IsActionEnabled(string actionType)
        {
            switch (actionType?.ToLower())
            {
                case "recruit":
                    return enableRecruit;
                case "drop_weapon":
                    return enableDropWeapon;
                case "romance":
                    return enableRomance;
                case "give_inspiration":
                case "inspiration":
                    return enableInspiration;
                case "force_rest":
                case "rest":
                    return enableRest;
                case "give_item":
                case "gift":
                    return enableGift;
                case "social_dining":
                case "dining":
                    return enableSocialDining;
                case "social_relax":
                case "relax":
                    return enableSocialRelax;
                default:
                    return true;
            }
        }

        /// <summary>
        /// 获取指定行为的成功率
        /// </summary>
        public float GetSuccessChance(string actionType)
        {
            switch (actionType?.ToLower())
            {
                case "recruit":
                    return recruitSuccessChance;
                case "drop_weapon":
                    return dropWeaponSuccessChance;
                case "romance":
                    return romanceSuccessChance;
                case "give_inspiration":
                case "inspiration":
                    return inspirationSuccessChance;
                case "force_rest":
                case "rest":
                    return restSuccessChance;
                case "give_item":
                case "gift":
                    return giftSuccessChance;
                case "social_dining":
                case "dining":
                    return socialDiningSuccessChance;
                case "social_relax":
                case "relax":
                    return socialRelaxSuccessChance;
                default:
                    return 1.0f;
            }
        }
    }
}
