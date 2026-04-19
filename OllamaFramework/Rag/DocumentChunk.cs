using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OllamaFramework.Rag
{
    /// <summary>
    /// 文档块及其嵌入向量的存储结构
    /// </summary>
    public class DocumentChunk
    {
        /// <summary>
        /// 文档块的文本内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 文档块的嵌入向量
        /// </summary>
        public float[] Embedding { get; set; }

        /// <summary>
        /// 文档块的元数据（如来源文件、分块索引、时间戳等）
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}