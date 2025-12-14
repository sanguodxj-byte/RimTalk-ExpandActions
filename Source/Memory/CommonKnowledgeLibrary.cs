using System;
using System.Collections.Generic;
using Verse;

namespace RimTalkExpandActions.Memory
{
    /// <summary>
    /// 常识库条目数据结构
    /// </summary>
    public class CommonKnowledgeEntry
    {
        /// <summary>
        /// 条目唯一标识符
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 标签分类
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// 重要性权重 (0.0 - 1.0)
        /// </summary>
        public float Importance { get; set; }

        /// <summary>
        /// 关键词列表（逗号分隔）
        /// </summary>
        public string Keywords { get; set; }

        /// <summary>
        /// 条目内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 目标 Pawn ID（-1 表示全局有效）
        /// </summary>
        public int TargetPawnId { get; set; } = -1;

        /// <summary>
        /// 创建时间戳
        /// </summary>
        public long Timestamp { get; set; }

        public CommonKnowledgeEntry()
        {
            Timestamp = DateTime.UtcNow.Ticks;
        }
    }

    /// <summary>
    /// 常识库管理类
    /// 如果您已有此类的实现，请删除此文件或合并功能
    /// </summary>
    public class CommonKnowledgeLibrary
    {
        private readonly List<CommonKnowledgeEntry> entries = new List<CommonKnowledgeEntry>();
        private readonly Dictionary<string, CommonKnowledgeEntry> entryById = new Dictionary<string, CommonKnowledgeEntry>();

        /// <summary>
        /// 添加新条目
        /// </summary>
        public void AddEntry(CommonKnowledgeEntry entry)
        {
            try
            {
                if (entry == null)
                {
                    Log.Error("[RimTalk-ExpandMemory] CommonKnowledgeLibrary.AddEntry: entry 为 null");
                    return;
                }

                if (string.IsNullOrEmpty(entry.Id))
                {
                    entry.Id = Guid.NewGuid().ToString();
                }

                // 检查 ID 是否已存在
                if (entryById.ContainsKey(entry.Id))
                {
                    Log.Warning($"[RimTalk-ExpandMemory] 条目 ID 已存在，覆盖: {entry.Id}");
                    RemoveEntry(entry.Id);
                }

                entries.Add(entry);
                entryById[entry.Id] = entry;

                Log.Message($"[RimTalk-ExpandMemory] 添加常识条目: {entry.Tag} - {entry.Id}");
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-ExpandMemory] AddEntry 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 通过 ID 获取条目
        /// </summary>
        public CommonKnowledgeEntry GetEntryById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            entryById.TryGetValue(id, out CommonKnowledgeEntry entry);
            return entry;
        }

        /// <summary>
        /// 获取所有条目
        /// </summary>
        public List<CommonKnowledgeEntry> GetAllEntries()
        {
            return new List<CommonKnowledgeEntry>(entries);
        }

        /// <summary>
        /// 移除条目
        /// </summary>
        public void RemoveEntry(string id)
        {
            try
            {
                if (entryById.TryGetValue(id, out CommonKnowledgeEntry entry))
                {
                    entries.Remove(entry);
                    entryById.Remove(id);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-ExpandMemory] RemoveEntry 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据关键词搜索条目
        /// </summary>
        public List<CommonKnowledgeEntry> SearchByKeywords(string query, int maxResults = 10)
        {
            var results = new List<CommonKnowledgeEntry>();

            try
            {
                if (string.IsNullOrEmpty(query))
                {
                    return results;
                }

                string normalizedQuery = query.ToLower();

                foreach (var entry in entries)
                {
                    if (entry.Keywords != null && entry.Keywords.ToLower().Contains(normalizedQuery))
                    {
                        results.Add(entry);
                        if (results.Count >= maxResults)
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-ExpandMemory] SearchByKeywords 失败: {ex.Message}");
            }

            return results;
        }
    }
}
