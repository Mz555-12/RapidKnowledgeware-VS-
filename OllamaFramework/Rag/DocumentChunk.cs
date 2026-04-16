
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
        public string Content { get; set; }
        public float[] Embedding { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
