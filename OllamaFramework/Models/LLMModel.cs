// ==================== Models/LLMModels.cs ====================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OllamaFramework.Models
{
    /// <summary>
    /// 大语言模型配置
    /// </summary>
    internal class LLMModel
    {
        /// <summary>
        /// 默认使用的对话模型名称
        /// </summary>
        public const string DefaultChatModel = "qwen2.5:3b";

        /// <summary>
        /// 自定义对话模型名称（可在运行时修改）
        /// </summary>
        public static string CurrentChatModel { get; set; } = DefaultChatModel;
    }

    /// <summary>
    /// LLM 生成参数（流式与非流式共用）
    /// </summary>
    public class LLMParameters
    {
        /// <summary>
        /// 温度参数，控制随机性（0.0 - 2.0），默认 0.7
        /// </summary>
        public float Temperature { get; set; } = 0.3f;

        /// <summary>
        /// Top-P 核采样参数（0.0 - 1.0），默认 0.9
        /// </summary>
        public float TopP { get; set; } = 0.5f;

        /// <summary>
        /// 上下文窗口大小（token 数量），默认 2048
        /// </summary>
        public int ContextSize { get; set; } = 4096;

        /// <summary>
        /// 系统提示词
        /// </summary>
        public string SystemPrompt { get; set; } = "You are a helpful assistant.";

        /// <summary>
        /// 拒绝响应的占位文本（当模型拒绝回答时返回此内容）
        /// </summary>
        public string RefusalResponse { get; set; } = "抱歉，我无法回答该问题。";

        /// <summary>
        /// 停止词列表，遇到即停止生成
        /// </summary>
        public List<string> StopWords { get; set; } = new List<string>();

        /// <summary>
        /// 是否启用流式输出
        /// </summary>
        public bool Stream { get; set; } = true;


        /// <summary>
        /// 随机种子（可选）
        /// </summary>
        public int? Seed { get; set; }

        /// <summary>
        /// 重复惩罚系数（可选）
        /// </summary>
        public float? RepeatPenalty { get; set; } = 1.0f;
    }
}