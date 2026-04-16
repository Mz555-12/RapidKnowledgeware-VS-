using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OllamaFramework.Models
{
    /// <summary>
    /// 嵌入模型配置
    /// </summary>
    internal class EmbeddingModel
    {
        /// <summary>
        /// 默认使用的嵌入模型名称
        /// </summary>
        public const string DefaultEmbeddingModel = "bge-m3:567m";

        /// <summary>
        /// 自定义嵌入模型名称（可在运行时修改）
        /// </summary>
        public static string CurrentEmbeddingModel { get; set; } = DefaultEmbeddingModel;
    }
}