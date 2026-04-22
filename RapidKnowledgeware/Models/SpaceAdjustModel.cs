using GalaSoft.MvvmLight;

namespace RapidKnowledgeware.Models
{
    /// <summary>
    /// 单个会话的空间参数模型（LLM 配置）
    /// </summary>
    public class SpaceAdjustModel : ObservableObject
    {
        private string _chatLLM = "qwen2.5:3b";
        /// <summary>
        /// 对话模型名称
        /// </summary>
        public string ChatLLM
        {
            get => _chatLLM;
            set { _chatLLM = value; RaisePropertyChanged(); }
        }

        private float _temperature = 0.3f;
        /// <summary>
        /// 温度参数（控制随机性）
        /// </summary>
        public float Temperature
        {
            get => _temperature;
            set { _temperature = value; RaisePropertyChanged(); }
        }

        private float _topP = 0.5f;
        /// <summary>
        /// Top-P 核采样参数
        /// </summary>
        public float TopP
        {
            get => _topP;
            set { _topP = value; RaisePropertyChanged(); }
        }

        private float _repeatPenalty = 1.0f;
        /// <summary>
        /// 重复惩罚系数
        /// </summary>
        public float RepeatPenalty
        {
            get => _repeatPenalty;
            set { _repeatPenalty = value; RaisePropertyChanged(); }
        }

        private string _queryRefusalResponse = "抱歉，我无法回答该问题。";
        /// <summary>
        /// 查询模式下的拒绝响应文本
        /// </summary>
        public string QueryRefusalResponse
        {
            get => _queryRefusalResponse;
            set { _queryRefusalResponse = value; RaisePropertyChanged(); }
        }

        private string _systemPrompt = "You are a helpful assistant.";
        /// <summary>
        /// 系统提示词
        /// </summary>
        public string SystemPrompt
        {
            get => _systemPrompt;
            set { _systemPrompt = value; RaisePropertyChanged(); }
        }


        private bool _isLinkKnowledgeBase = true;
        /// <summary>
        /// 是否对接知识库（默认开启）
        /// </summary>
        public bool IsLinkKnowledgeBase
        {
            get => _isLinkKnowledgeBase;
            set { _isLinkKnowledgeBase = value; RaisePropertyChanged(); }
        }


        private int _searchQuantity = 10;
        /// <summary>
        /// 检索返回的文档块数量（会话级）
        /// </summary>
        public int SearchQuantity
        {
            get => _searchQuantity;
            set { _searchQuantity = value; RaisePropertyChanged(); }
        }

        private float _indexSimilarityThreshold = 0.6f;
        /// <summary>
        /// 相似度阈值，0~1（会话级）
        /// </summary>
        public float IndexSimilarityThreshold
        {
            get => _indexSimilarityThreshold;
            set { _indexSimilarityThreshold = value; RaisePropertyChanged(); }
        }
    }
}