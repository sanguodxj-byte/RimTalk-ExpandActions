using System;
using UnityEngine;
using Verse;
using RimWorld;

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
                // 临时禁用UI，直接显示简单文本
                Widgets.Label(new Rect(0f, 0f, inRect.width, 60f), 
                    "RimTalk-ExpandActions\n设置功能开发中，请通过代码配置。");
                // RimTalkExpandActionsSettingsUI.DoSettingsWindowContents(inRect, Settings);
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("[RimTalk-ExpandActions] 设置界面错误: {0}", ex.Message));
            }
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

        // ===== 高级设置 =====

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

            // 成功难度系数
            Scribe_Values.Look(ref recruitSuccessChance, "recruitSuccessChance", 1.0f);
            Scribe_Values.Look(ref dropWeaponSuccessChance, "dropWeaponSuccessChance", 1.0f);
            Scribe_Values.Look(ref romanceSuccessChance, "romanceSuccessChance", 1.0f);
            Scribe_Values.Look(ref inspirationSuccessChance, "inspirationSuccessChance", 1.0f);
            Scribe_Values.Look(ref restSuccessChance, "restSuccessChance", 1.0f);
            Scribe_Values.Look(ref giftSuccessChance, "giftSuccessChance", 1.0f);

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

            recruitSuccessChance = 1.0f;
            dropWeaponSuccessChance = 1.0f;
            romanceSuccessChance = 1.0f;
            inspirationSuccessChance = 1.0f;
            restSuccessChance = 1.0f;
            giftSuccessChance = 1.0f;

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
                default:
                    return 1.0f;
            }
        }
    }
}
