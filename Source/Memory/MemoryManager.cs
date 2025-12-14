using System;
using Verse;

namespace RimTalkExpandActions.Memory
{
    /// <summary>
    /// 内存管理器单例类
    /// 如果您已有此类的实现，请删除此文件或合并功能
    /// </summary>
    public class MemoryManager
    {
        private static MemoryManager instance;
        private static readonly object lockObj = new object();

        /// <summary>
        /// 单例实例
        /// </summary>
        public static MemoryManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObj)
                    {
                        if (instance == null)
                        {
                            instance = new MemoryManager();
                            Log.Message("[RimTalk-ExpandMemory] MemoryManager 单例初始化完成");
                        }
                    }
                }
                return instance;
            }
        }

        /// <summary>
        /// 常识库实例
        /// </summary>
        public CommonKnowledgeLibrary CommonKnowledge { get; private set; }

        /// <summary>
        /// 私有构造函数
        /// </summary>
        private MemoryManager()
        {
            try
            {
                CommonKnowledge = new CommonKnowledgeLibrary();
                Log.Message("[RimTalk-ExpandMemory] MemoryManager 初始化成功");
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-ExpandMemory] MemoryManager 初始化失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 重置单例实例（用于测试或重载）
        /// </summary>
        public static void Reset()
        {
            lock (lockObj)
            {
                instance = null;
                Log.Message("[RimTalk-ExpandMemory] MemoryManager 已重置");
            }
        }
    }
}
