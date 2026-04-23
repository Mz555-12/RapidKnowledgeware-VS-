using System;
using System.Globalization;
using System.Windows.Data;

namespace RapidKnowledgeware.Converters
{
    /// <summary>
    /// 根据深度思考开关状态选择显示的模型名称
    /// 绑定顺序：[0] ChatLLM, [1] DeepThinkingLLM, [2] GlobalIsDeepThinking
    /// </summary>
    public class ModelDisplayConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3) return "未选择模型";

            string chatLLM = values[0] as string;
            string deepThinkingLLM = values[1] as string;
            bool isDeepThinking = values[2] is bool b && b;

            if (!isDeepThinking)
                return chatLLM ?? "未选择模型";

            // 深度思考开启
            if (string.IsNullOrWhiteSpace(deepThinkingLLM))
                return "未配置思考模型";
            return deepThinkingLLM;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}