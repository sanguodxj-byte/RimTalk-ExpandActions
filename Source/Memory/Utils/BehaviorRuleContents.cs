using System.Collections.Generic;

namespace RimTalkExpandActions.Memory.Utils
{
    /// <summary>
    /// 行为规则内容定义
    /// 存储所有系统指令的标准内容文本
    /// </summary>
    public static class BehaviorRuleContents
    {
        /// <summary>
        /// 招募规则内容
        /// </summary>
        public const string RECRUIT_RULE = @"当谈话涉及【招募、加入、投靠、派系】话题，且目标NPC在对话中明确表示同意加入玩家派系时（例如说""我愿意加入""、""好吧，我跟你走""），请务必在回复的最后附加如下JSON代码：{""action"": ""recruit"", ""target"": ""NPC名字""}。注意：仅在对方明确同意时才输出此指令，拒绝或犹豫时不输出。";

        /// <summary>
        /// 投降/丢弃武器规则内容
        /// </summary>
        public const string DROP_WEAPON_RULE = @"当对话涉及【投降、放下武器、认输、别杀我】等话题，且NPC明确表示愿意投降或放下武器时，请在回复末尾附加JSON：{""action"": ""drop_weapon"", ""target"": ""NPC名字""}。
示例场景：
- 玩家威胁：""放下武器，否则开枪！""
- NPC回应：""好好好，我投降！别开枪！{""action"": ""drop_weapon"", ""target"": ""张三""}""
注意：NPC必须明确表达投降意愿才输出指令。";

        /// <summary>
        /// 恋爱关系规则内容
        /// </summary>
        public const string ROMANCE_RULE = @"当对话涉及【爱、喜欢、做我女朋友/男朋友、在一起、分手】等话题时：
1. 如果双方确立恋爱关系，输出：{""action"": ""romance"", ""target"": ""NPC名字"", ""partner"": ""另一方名字"", ""type"": ""new_lover""}
2. 如果决定分手，输出：{""action"": ""romance"", ""target"": ""NPC名字"", ""partner"": ""另一方名字"", ""type"": ""breakup""}

示例：
- 确立关系：""我也喜欢你，我们在一起吧！{""action"": ""romance"", ""target"": ""艾莉丝"", ""partner"": ""玩家角色"", ""type"": ""new_lover""}""
- 分手：""对不起，我们不合适，还是分开吧...{""action"": ""romance"", ""target"": ""艾莉丝"", ""partner"": ""玩家角色"", ""type"": ""breakup""}""

注意：必须双方都同意才输出指令。";

        /// <summary>
        /// 灵感触发规则内容
        /// </summary>
        public const string INSPIRATION_RULE = @"当对话涉及【灵感、启发、顿悟、加油、鼓励】等激励性话题，且NPC受到鼓舞时，根据场景输出对应灵感JSON：
1. 战斗相关 → {""action"": ""give_inspiration"", ""target"": ""NPC名字"", ""type"": ""frenzy_shoot""}
2. 工作相关 → {""action"": ""give_inspiration"", ""target"": ""NPC名字"", ""type"": ""frenzy_work""}
3. 交易相关 → {""action"": ""give_inspiration"", ""target"": ""NPC名字"", ""type"": ""inspired_trade""}

示例：
- 战斗鼓励：""你可以的！专心瞄准！"" → NPC回应：""我感觉状态来了！{""action"": ""give_inspiration"", ""target"": ""李四"", ""type"": ""frenzy_shoot""}""
- 工作激励：""加油干，我相信你！"" → NPC回应：""好！我今天一定多干活！{""action"": ""give_inspiration"", ""target"": ""李四"", ""type"": ""frenzy_work""}""

注意：灵感应基于对话内容自然触发。";

        /// <summary>
        /// 休息规则内容
        /// </summary>
        public const string REST_RULE = @"当对话涉及【休息、睡觉、困了、累了】等话题时：
1. 正常休息（去床上睡觉）→ {""action"": ""force_rest"", ""target"": ""NPC名字"", ""immediate"": false}
2. 极度疲劳/昏迷 → {""action"": ""force_rest"", ""target"": ""NPC名字"", ""immediate"": true}

示例：
- 正常休息：""好累啊，我去睡一会儿。{""action"": ""force_rest"", ""target"": ""王五"", ""immediate"": false}""
- 突然昏迷：""我...我好困...撑不住了...{""action"": ""force_rest"", ""target"": ""王五"", ""immediate"": true}""

注意：immediate=true 仅用于极端疲劳场景。";

        /// <summary>
        /// 赠送物品规则内容
        /// </summary>
        public const string GIFT_RULE = @"当对话涉及【给你、赠送、礼物、拿去、送你】等赠送行为，且NPC明确表示要赠送某物时，输出JSON：{""action"": ""give_item"", ""target"": ""NPC名字"", ""item_keyword"": ""物品关键词""}

item_keyword 应为物品名称的关键部分，例如：
- ""药"" → 匹配背包中的药品
- ""枪"" → 匹配武器
- ""银"" → 匹配银币
- ""食物"" → 匹配食物

示例：
- ""这把枪送给你吧，我用不上了。{""action"": ""give_item"", ""target"": ""赵六"", ""item_keyword"": ""枪""}""
- ""拿点药吧，你受伤了。{""action"": ""give_item"", ""target"": ""赵六"", ""item_keyword"": ""药""}""

注意：NPC必须确实拥有该物品才能赠送。";

        /// <summary>
        /// 社交用餐行为规则
        /// </summary>
        public const string SOCIAL_DINING_RULE = @"当对话双方决定一起吃饭、分享食物、举办小型聚餐或庆祝时，且双方关系良好，请在回复末尾附加JSON：{""action"": ""social_dining"", ""target"": ""(对方名字)""}。注意：仅在确认要执行此行动时输出。";

        /// <summary>
        /// 获取所有规则定义
        /// </summary>
        public static Dictionary<string, RuleDefinition> GetAllRules()
        {
            return new Dictionary<string, RuleDefinition>
            {
                {
                    "sys-rule-recruit",
                    new RuleDefinition
                    {
                        Id = "sys-rule-recruit",
                        Tag = "系统指令",
                        Content = RECRUIT_RULE,
                        Keywords = new[] { "招募", "加入", "投靠", "派系", "跟我走", "收留", "收编", "归顺" },
                        Importance = 1.0f
                    }
                },
                {
                    "sys-rule-drop-weapon",
                    new RuleDefinition
                    {
                        Id = "sys-rule-drop-weapon",
                        Tag = "系统指令",
                        Content = DROP_WEAPON_RULE,
                        Keywords = new[] { "投降", "放下武器", "认输", "别杀我", "饶命", "缴械" },
                        Importance = 1.0f
                    }
                },
                {
                    "sys-rule-romance",
                    new RuleDefinition
                    {
                        Id = "sys-rule-romance",
                        Tag = "系统指令",
                        Content = ROMANCE_RULE,
                        Keywords = new[] { "爱", "喜欢", "做我女朋友", "做我男朋友", "分手", "在一起", "表白", "恋爱" },
                        Importance = 1.0f
                    }
                },
                {
                    "sys-rule-inspiration",
                    new RuleDefinition
                    {
                        Id = "sys-rule-inspiration",
                        Tag = "系统指令",
                        Content = INSPIRATION_RULE,
                        Keywords = new[] { "灵感", "启发", "顿悟", "加油", "鼓励", "激励", "状态" },
                        Importance = 1.0f
                    }
                },
                {
                    "sys-rule-rest",
                    new RuleDefinition
                    {
                        Id = "sys-rule-rest",
                        Tag = "系统指令",
                        Content = REST_RULE,
                        Keywords = new[] { "休息", "睡觉", "昏迷", "好困", "累了", "疲劳", "躺下" },
                        Importance = 1.0f
                    }
                },
                {
                    "sys-rule-gift",
                    new RuleDefinition
                    {
                        Id = "sys-rule-gift",
                        Tag = "系统指令",
                        Content = GIFT_RULE,
                        Keywords = new[] { "送给", "给你", "拿去", "送去", "赠送", "给", "拿" },
                        Importance = 1.0f
                    }
                },
                {
                    "sys-rule-social-dining",
                    new RuleDefinition
                    {
                        Id = "sys-rule-social-dining",
                        Tag = "系统指令",
                        Content = SOCIAL_DINING_RULE,
                        Keywords = new[] { "吃饭", "聚餐", "饿了", "分享食物", "吃点东西", "庆祝", "喝一杯", "共进晚餐", "一起吃", "用餐" },
                        Importance = 1.0f
                    }
                }
            };
        }
    }

    /// <summary>
    /// 规则定义数据结构
    /// </summary>
    public class RuleDefinition
    {
        public string Id { get; set; }
        public string Tag { get; set; }
        public string Content { get; set; }
        public string[] Keywords { get; set; }
        public float Importance { get; set; }
    }
}
