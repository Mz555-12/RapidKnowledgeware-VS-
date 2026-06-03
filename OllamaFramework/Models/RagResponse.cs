using OllamaFramework.Models;
using System.Collections.Generic;

namespace OllamaFramework.Models
{
    public class RagResponse
    {
        /// <summary>
        /// 生成的回答文本
        /// </summary>
        public string Answer { get; set; }

        /// <summary>
        /// 检索到的相关文档块及其相似度
        /// </summary>
        public List<(DocumentChunk Chunk, float Similarity)> RetrievedChunks { get; set; }

        /// <summary>
        /// 实际发送给 LLM 的增强提示词（调试用）
        /// </summary>
        public string PromptUsed { get; set; }
    }
}
