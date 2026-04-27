using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;
using RapidKnowledgeware.Models;

namespace RapidKnowledgeware.Converters
{
    /// <summary>
    /// 将 AI 回复字符串解析为 MessageBlock 列表（区分文本和代码块）
    /// </summary>
    public class MessageContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var content = value as string;
            var blocks = new List<MessageBlock>();

            if (string.IsNullOrEmpty(content))
            {
                blocks.Add(new MessageBlock { Type = "Text", Content = "" });
                return blocks;
            }

            var regex = new Regex(@"```(\w*)\s*([\s\S]*?)```", RegexOptions.Multiline);
            int lastIndex = 0;
            foreach (Match match in regex.Matches(content))
            {
                // 前面的普通文本
                if (match.Index > lastIndex)
                {
                    string text = content.Substring(lastIndex, match.Index - lastIndex).Trim('\r', '\n', ' ');
                    if (!string.IsNullOrEmpty(text))
                        blocks.Add(new MessageBlock { Type = "Text", Content = text });
                }

                // 代码块
                blocks.Add(new MessageBlock
                {
                    Type = "Code",
                    Language = match.Groups[1].Value,
                    Content = match.Groups[2].Value.TrimEnd()
                });

                lastIndex = match.Index + match.Length;
            }

            // 剩余文本
            if (lastIndex < content.Length)
            {
                string text = content.Substring(lastIndex).Trim('\r', '\n', ' ');
                if (!string.IsNullOrEmpty(text))
                    blocks.Add(new MessageBlock { Type = "Text", Content = text });
            }

            if (blocks.Count == 0)
                blocks.Add(new MessageBlock { Type = "Text", Content = content });

            return blocks;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}