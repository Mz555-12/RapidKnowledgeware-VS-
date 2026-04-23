using OllamaFramework.Rag;
using RapidKnowledgeware.Functions.DebugFunc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RapidKnowledgeware.Functions.ChatFunc
{
    /// <summary>
    /// 提示词智能截断服务：根据上下文窗口大小，按相似度优先级截取文档块内容
    /// </summary>
    public static class PromptTruncationService
    {
        /// <summary>
        /// 根据上下文窗口上限，从检索结果中截取内容并构建增强提示词（确保最终提示词总长度不超过限制）
        /// </summary>
        /// <param name="query">用户原始问题</param>
        /// <param name="retrievedChunks">已按相似度降序排列的检索结果</param>
        /// <param name="maxContextLength">上下文窗口最大字符数（整个提示词的总长度限制）</param>
        /// <param name="promptTemplate">提示词模板，占位符 {context} 和 {question}</param>
        /// <returns>截断后的增强提示词</returns>
        public static string BuildTruncatedPrompt(string query, List<(DocumentChunk Chunk, float Similarity)> retrievedChunks, int maxContextLength, string promptTemplate = null)
        {
            if (retrievedChunks == null || retrievedChunks.Count == 0)
                return query;

            if (string.IsNullOrEmpty(promptTemplate))
            {
                promptTemplate =
                    "你是一个知识库助手，请严格根据以下提供的上下文信息回答问题。\n" +
                    "如果上下文不足以回答，请明确告知用户，不要编造内容。\n\n" +
                    "上下文：\n{context}\n\n" +
                    "问题：{question}\n" +
                    "回答：";
            }

            // 计算模板固定部分长度（将占位符替换为空）
            string templateWithoutPlaceholders = promptTemplate.Replace("{context}", "").Replace("{question}", "");
            int fixedLength = templateWithoutPlaceholders.Length + query.Length;

            // 可用于上下文内容的剩余字符数
            int availableForContext = maxContextLength - fixedLength;
            if (availableForContext <= 0)
            {
                // 连固定文本都放不下，返回最小可用提示词
                return promptTemplate.Replace("{context}", "").Replace("{question}", query);
            }

            var contextBuilder = new StringBuilder();
            int usedContextChars = 0;
            int usedChunkCount = 0;

            // 用于调试统计
            var usedList = new List<string>();
            var cutDetails = new List<(string ChunkId, string KeptContent, string CutContent, int OriginalLen, int KeptLen)>();
            var skippedList = new List<string>();

            foreach (var item in retrievedChunks)
            {
                string chunkContent = item.Chunk.Content;
                int chunkLength = chunkContent.Length;

                string sourceFile = Path.GetFileName(item.Chunk.Metadata["source"]?.ToString() ?? "未知");
                string chunkIndex = item.Chunk.Metadata["chunk_index"]?.ToString() ?? "?";
                string chunkId = $"{sourceFile}[{chunkIndex}]";

                // 构建当前片段头部（例如 "[片段 1] (来源: xxx, 相似度: 0.9876)"）
                string header = $"[片段 {usedChunkCount + 1}] (来源: {sourceFile}, 相似度: {item.Similarity:F4})";
                string headerWithNewline = header + Environment.NewLine;
                int headerLength = headerWithNewline.Length;

                // 计算添加整个块需要的总空间（头部 + 内容 + 可能尾部换行）
                int chunkTotalLength = headerLength + chunkLength + Environment.NewLine.Length;

                // 剩余可用上下文字符数
                int remaining = availableForContext - usedContextChars;
                if (remaining <= 0)
                {
                    skippedList.Add($"{chunkId}({chunkLength}字)");
                    continue;
                }

                if (chunkTotalLength <= remaining)
                {
                    // 完整加入
                    contextBuilder.Append(headerWithNewline);
                    contextBuilder.AppendLine(chunkContent);
                    usedContextChars += chunkTotalLength;
                    usedChunkCount++;
                    usedList.Add($"{chunkId}({chunkLength}字)");
                }
                else
                {
                    // 需要截断内容部分
                    int availableForContent = remaining - headerLength - Environment.NewLine.Length;
                    if (availableForContent > 0)
                    {
                        string keptContent = chunkContent.Substring(0, availableForContent);
                        string cutContent = chunkContent.Substring(availableForContent);
                        contextBuilder.Append(headerWithNewline);
                        contextBuilder.AppendLine(keptContent);
                        usedContextChars += headerLength + availableForContent + Environment.NewLine.Length;
                        usedChunkCount++;

                        cutDetails.Add((chunkId, keptContent, cutContent, chunkLength, availableForContent));
                    }
                    else
                    {
                        skippedList.Add($"{chunkId}({chunkLength}字)");
                    }
                    break; // 一旦发生截断，后续块不再处理（因为空间已满）
                }
            }

            string contextText = contextBuilder.ToString().Trim();
            string finalPrompt = promptTemplate.Replace("{context}", contextText).Replace("{question}", query);

            // === 调试输出（增强截断信息） ===
            string summary = $"上下文窗口:{maxContextLength}字 | 提示词实际总长:{finalPrompt.Length}字 | 已用上下文字符:{usedContextChars}字";
            string usedPart = usedList.Count > 0 ? $"完整块:{string.Join(", ", usedList)}" : "完整块:无";

            string cutPart = "截断块:无";
            if (cutDetails.Any())
            {
                var cutSb = new StringBuilder();
                foreach (var (id, kept, cut, origLen, keptLen) in cutDetails)
                {
                    cutSb.AppendLine($"############### {id} ###################");
                    cutSb.AppendLine($"\n{id}(原{origLen}字→保留{keptLen}字)\n");
                    cutSb.AppendLine("############ 保留内容 ################");
                    cutSb.AppendLine($"\n保留内容：{kept}\n");
                    cutSb.AppendLine("############ 截断内容 ################");
                    cutSb.AppendLine($"\n截断内容：{cut}\n");
                    cutSb.AppendLine($"####################################");
                }
                cutPart = cutSb.ToString().TrimEnd();
            }

            string skippedPart = skippedList.Count > 0 ? $"跳过块:{string.Join(", ", skippedList)}" : "跳过块:无";
            DebugService.Info($"{summary}\n{usedPart}\n\n{cutPart}\n\n{skippedPart}");
            // ===============================

            return finalPrompt;
        }
    }
}