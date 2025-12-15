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
                Listing_Standard listingStandard = new Listing_Standard();
                listingStandard.Begin(inRect);
                
                // === 标题 ===
                Text.Font = GameFont.Medium;
                listingStandard.Label("RimTalk-ExpandActions v1.1.0");
                Text.Font = GameFont.Small;
                listingStandard.Gap();
                
                // === 全局设置 ===
                listingStandard.Label("━━━━━ 全局设置 ━━━━━");
                listingStandard.Gap(6f);
                
                listingStandard.CheckboxLabeled("自动注入规则到常识库", ref Settings.autoInjectRules, 
                    "启动游戏时自动将所有行为规则注入到 RimTalk-ExpandMemory 的常识库");
                
                listingStandard.CheckboxLabeled("显示行为触发消息", ref Settings.showActionMessages,
                    "在游戏中显示行为触发的提示消息");
                
                listingStandard.CheckboxLabeled("启用详细日志", ref Settings.enableDetailedLogging,
                    "在日志中记录详细的执行过程（用于调试）");
                
                listingStandard.Gap();
                listingStandard.Label($"规则重要性: {Settings.ruleImportance:F1}");
                Settings.ruleImportance = listingStandard.Slider(Settings.ruleImportance, 0f, 2f);
                
                listingStandard.Gap();
                
                // === 行为开关 ===
                listingStandard.Label("━━━━━ 行为开关 ━━━━━");
                listingStandard.Gap(6f);
                
                listingStandard.CheckboxLabeled("? 招募系统", ref Settings.enableRecruit,
                    "通过对话招募 NPC 到殖民地");
                
                listingStandard.CheckboxLabeled("? 社交用餐", ref Settings.enableSocialDining,
                    "邀请他人共进晚餐，增进关系");
                
                listingStandard.CheckboxLabeled("? 投降/丢武器", ref Settings.enableDropWeapon,
                    "让敌人放下武器投降");
                
                listingStandard.CheckboxLabeled("? 恋爱关系", ref Settings.enableRomance,
                    "建立或结束恋人关系");
                
                listingStandard.CheckboxLabeled("? 灵感触发", ref Settings.enableInspiration,
                    "给予角色工作/战斗/交易灵感");
                
                listingStandard.CheckboxLabeled("? 强制休息", ref Settings.enableRest,
                    "让角色去休息或陷入昏迷");
                
                listingStandard.CheckboxLabeled("? 赠送物品", ref Settings.enableGift,
                    "从背包中赠送物品给他人");
                
                listingStandard.Gap();
                
                // === 成功率设置 ===
                listingStandard.Label("━━━━━ 成功率设置 ━━━━━");
                listingStandard.Gap(6f);
                
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
                
                listingStandard.Gap();
                
                // === 操作按钮 ===
                listingStandard.Label("━━━━━ 操作 ━━━━━");
                listingStandard.Gap(6f);
                
                if (listingStandard.ButtonText("重置为默认值"))
                {
                    Settings.ResetToDefault();
                    Messages.Message("设置已重置为默认值", MessageTypeDefOf.NeutralEvent);
                }
                
                if (listingStandard.ButtonText("手动注入规则到常识库"))
                {
                    // 调用注入器
                    try
                    {
                        var allRules = Memory.Utils.BehaviorRuleContents.GetAllRules();
                        int successCount = 0;
                        
                        foreach (var ruleKvp in allRules)
                        {
                            bool success = Memory.Utils.CrossModRecruitRuleInjector.TryInjectRule(
                                ruleKvp.Value.Id,
                                ruleKvp.Value.Tag,
                                ruleKvp.Value.Content,
                                ruleKvp.Value.Keywords,
                                ruleKvp.Value.Importance
                            );
                            
                            if (success) successCount++;
                        }
                        
                        Messages.Message($"成功注入 {successCount} 条规则", MessageTypeDefOf.PositiveEvent);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[RimTalk-ExpandActions] 手动注入失败: {ex.Message}");
                        Messages.Message("注入失败，请查看日志", MessageTypeDefOf.RejectInput);
                    }
                }
                
                listingStandard.Gap();
                listingStandard.Label("提示：修改设置后会自动保存");
                
                listingStandard.End();
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-ExpandActions] 设置界面错误: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private static Vector2 scrollPosition;
        private static Rect viewRect = new Rect(0f, 0f, 600f, 400f);

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
        /// 是否启用自动注入规则
        /// </summary>
        public bool autoInjectRules = true;

        /// <summary>
        /// 规则重要性（影响 AI 检索优先级）
        /// </summary>
        public float ruleImportance = 1.0f;

        /// <summary>
        /// 自定义招募规则内容
        /// </summary>
        public string customRecruitRuleContent = "";

        /// <summary>
        /// 上次手动注入时间（用于防止重复点击）
        /// </summary>
        public long lastManualInjectTime = 0;

        // ===== 行为开关 =====

        /// <summary>
        /// 启用招募功能
        /// </summary>
        public bool enableRecruit = true;

        /// <summary>
        /// 启用投降/丢武器功能
        /// </summary>
        public bool enableDropWeapon = true;

        /// <summary>
        /// 启用恋爱关系功能
        /// </summary>
        public bool enableRomance = true;

        /// <summary>
        /// 启用灵感触发功能
        /// </summary>
        public bool enableInspiration = true;

        /// <summary>
        /// 启用休息/昏迷功能
        /// </summary>
        public bool enableRest = true;

        /// <summary>
        /// 启用物品赠送功能
        /// </summary>
        public bool enableGift = true;

        /// <summary>
        /// 启用社交用餐功能
        /// </summary>
        public bool enableSocialDining = true;

        // ===== 成功难度系数 (0.0 - 1.0) =====

        /// <summary>
        /// 招募成功率系数（0.0 = 必定失败, 1.0 = 100%成功）
        /// </summary>
        public float recruitSuccessChance = 1.0f;

        /// <summary>
        /// 投降成功率系数
        /// </summary>
        public float dropWeaponSuccessChance = 1.0f;

        /// <summary>
        /// 恋爱成功率系数
        /// </summary>
        public float romanceSuccessChance = 1.0f;

        /// <summary>
        /// 灵感触发成功率系数
        /// </summary>
        public float inspirationSuccessChance = 1.0f;

        /// <summary>
        /// 休息成功率系数
        /// </summary>
        public float restSuccessChance = 1.0f;

        /// <summary>
        /// 赠送成功率系数
        /// </summary>
        public float giftSuccessChance = 1.0f;

        /// <summary>
        /// 社交用餐成功率系数
        /// </summary>
        public float socialDiningSuccessChance = 1.0f;

        // ===== 逻辑配置 =====
        /// <summary>
        /// 是否显示详细日志
        /// </summary>
        public bool enableDetailedLogging = false;

        /// <summary>
        /// 是否在游戏中显示行为触发提示
        /// </summary>
        public bool showActionMessages = true;

        public override void ExposeData()
        {
            base.ExposeData();
            
            // 全局设置
            Scribe_Values.Look(ref autoInjectRules, "autoInjectRules", true);
            Scribe_Values.Look(ref ruleImportance, "ruleImportance", 1.0f);
            Scribe_Values.Look(ref customRecruitRuleContent, "customRecruitRuleContent", "");
            Scribe_Values.Look(ref lastManualInjectTime, "lastManualInjectTime", 0L);

            // 行为开关
            Scribe_Values.Look(ref enableRecruit, "enableRecruit", true);
            Scribe_Values.Look(ref enableDropWeapon, "enableDropWeapon", true);
            Scribe_Values.Look(ref enableRomance, "enableRomance", true);
            Scribe_Values.Look(ref enableInspiration, "enableInspiration", true);
            Scribe_Values.Look(ref enableRest, "enableRest", true);
            Scribe_Values.Look(ref enableGift, "enableGift", true);
            Scribe_Values.Look(ref enableSocialDining, "enableSocialDining", true);

            // 成功难度系数
            Scribe_Values.Look(ref recruitSuccessChance, "recruitSuccessChance", 1.0f);
            Scribe_Values.Look(ref dropWeaponSuccessChance, "dropWeaponSuccessChance", 1.0f);
            Scribe_Values.Look(ref romanceSuccessChance, "romanceSuccessChance", 1.0f);
            Scribe_Values.Look(ref inspirationSuccessChance, "inspirationSuccessChance", 1.0f);
            Scribe_Values.Look(ref restSuccessChance, "restSuccessChance", 1.0f);
            Scribe_Values.Look(ref giftSuccessChance, "giftSuccessChance", 1.0f);
            Scribe_Values.Look(ref socialDiningSuccessChance, "socialDiningSuccessChance", 1.0f);

            // 高级设置
            Scribe_Values.Look(ref enableDetailedLogging, "enableDetailedLogging", false);
            Scribe_Values.Look(ref showActionMessages, "showActionMessages", true);
        }

        /// <summary>
        /// 重置为默认值
        /// </summary>
        public void ResetToDefault()
        {
            autoInjectRules = true;
            ruleImportance = 1.0f;
            customRecruitRuleContent = "";

            enableRecruit = true;
            enableDropWeapon = true;
            enableRomance = true;
            enableInspiration = true;
            enableRest = true;
            enableGift = true;
            enableSocialDining = true;

            recruitSuccessChance = 1.0f;
            dropWeaponSuccessChance = 1.0f;
            romanceSuccessChance = 1.0f;
            inspirationSuccessChance = 1.0f;
            restSuccessChance = 1.0f;
            giftSuccessChance = 1.0f;
            socialDiningSuccessChance = 1.0f;

            enableDetailedLogging = false;
            showActionMessages = true;
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
                default:
                    return 1.0f;
            }
        }
    }
}
