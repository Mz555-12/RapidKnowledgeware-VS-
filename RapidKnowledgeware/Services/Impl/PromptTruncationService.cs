using IOC;
using IOC.Annotations;
using OllamaFramework.Models;
using OllamaFramework.Rag;
using RapidKnowledgeware.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RapidKnowledgeware.Services.Impl
{
    [Service]
    public class PromptTruncationService : IPromptTruncationService
    {
        public string BuildTruncatedPrompt(string query, List<(DocumentChunk Chunk, float Similarity)> retrievedChunks, int maxContextLength, string promptTemplate = null)
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

            string templateWithoutPlaceholders = promptTemplate.Replace("{context}", "").Replace("{question}", "");
            int fixedLength = templateWithoutPlaceholders.Length + query.Length;

            int availableForContext = maxContextLength - fixedLength;
            if (availableForContext <= 0)
            {
                return promptTemplate.Replace("{context}", "").Replace("{question}", query);
            }

            var contextBuilder = new StringBuilder();
            int usedContextChars = 0;
            int usedChunkCount = 0;

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

                string header = $"[片段 {usedChunkCount + 1}] (来源: {sourceFile}, 相似度: {item.Similarity:F4})";
                string headerWithNewline = header + Environment.NewLine;
                int headerLength = headerWithNewline.Length;

                int chunkTotalLength = headerLength + chunkLength + Environment.NewLine.Length;

                int remaining = availableForContext - usedContextChars;
                if (remaining <= 0)
                {
                    skippedList.Add($"{chunkId}({chunkLength}字)");
                    continue;
                }

                if (chunkTotalLength <= remaining)
                {
                    contextBuilder.Append(headerWithNewline);
                    contextBuilder.AppendLine(chunkContent);
                    usedContextChars += chunkTotalLength;
                    usedChunkCount++;
                    usedList.Add($"{chunkId}({chunkLength}字)");
                }
                else
                {
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
                    break;
                }
            }

            string contextText = contextBuilder.ToString().Trim();
            string finalPrompt = promptTemplate.Replace("{context}", contextText).Replace("{question}", query);

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
            BeanFactory.GetBean<IDebugService>().Info($"{summary}\n{usedPart}\n\n{cutPart}\n\n{skippedPart}");

            return finalPrompt;
        }

        public string BuildTruncatedContext(
            List<(DocumentChunk Chunk, float Similarity)> retrievedChunks,
            int maxContextLength)
        {
            if (retrievedChunks == null || retrievedChunks.Count == 0)
                return string.Empty;

            int availableForContext = maxContextLength;
            if (availableForContext <= 0)
                return string.Empty;

            var contextBuilder = new StringBuilder();
            int usedContextChars = 0;
            int usedChunkCount = 0;

            foreach (var item in retrievedChunks)
            {
                string chunkContent = item.Chunk.Content;
                int chunkLength = chunkContent.Length;

                string sourceFile = Path.GetFileName(item.Chunk.Metadata["source"]?.ToString() ?? "未知");
                string chunkIndex = item.Chunk.Metadata["chunk_index"]?.ToString() ?? "?";

                string header = $"[片段 {usedChunkCount + 1}] (来源: {sourceFile}, 相似度: {item.Similarity:F4})";
                string headerWithNewline = header + Environment.NewLine;
                int headerLength = headerWithNewline.Length;
                int chunkTotalLength = headerLength + chunkLength + Environment.NewLine.Length;

                int remaining = availableForContext - usedContextChars;
                if (remaining <= 0)
                    break;

                if (chunkTotalLength <= remaining)
                {
                    contextBuilder.Append(headerWithNewline);
                    contextBuilder.AppendLine(chunkContent);
                    usedContextChars += chunkTotalLength;
                    usedChunkCount++;
                }
                else
                {
                    int availableForContent = remaining - headerLength - Environment.NewLine.Length;
                    if (availableForContent > 0)
                    {
                        string keptContent = chunkContent.Substring(0, availableForContent);
                        contextBuilder.Append(headerWithNewline);
                        contextBuilder.AppendLine(keptContent);
                        usedContextChars += headerLength + availableForContent + Environment.NewLine.Length;
                        usedChunkCount++;
                    }
                    break;
                }
            }

            return contextBuilder.ToString().Trim();
        }
    }
}
