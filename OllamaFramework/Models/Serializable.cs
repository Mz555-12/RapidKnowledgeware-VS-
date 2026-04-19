using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OllamaFramework.Models
{
    /// <summary>
    /// 序列化数据容器类
    /// </summary>
    public class Serializable
    {
        /// <summary>
        /// 索引数据结构，用于持久化文档块集合
        /// </summary>
        public class IndexData
        {
            /// <summary>
            /// 文档块数据列表
            /// </summary>
            public List<ChunkData> Chunks { get; set; }
        }

        /// <summary>
        /// 单个文档块的可序列化数据结构
        /// </summary>
        public class ChunkData
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
            /// 文档块的元数据字典
            /// </summary>
            public Dictionary<string, object> Metadata { get; set; }
        }
    }
}