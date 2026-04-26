// KnowledgeBaseModel.cs
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace RapidKnowledgeware.Models
{
    /// <summary>
    /// 知识库数据模型（单例）
    /// </summary>
    public class KnowledgeBaseModel : ObservableObject
    {
        private static KnowledgeBaseModel _instance;
        /// <summary>
        /// 知识库模型单例实例
        /// </summary>
        public static KnowledgeBaseModel Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new KnowledgeBaseModel();
                return _instance;
            }
        }
        private KnowledgeBaseModel() { }

        private string _defalut_currentEmbeddingName = "bge-m3:567m";
        /// <summary>
        /// 当前使用的嵌入模型名称
        /// </summary>
        public string Default_CurrentEmbeddingName
        {
            get => _defalut_currentEmbeddingName;
            set { _defalut_currentEmbeddingName = value; RaisePropertyChanged(); }
        }

        private string _defalut_blockRule = "###";
        /// <summary>
        /// 全局分块规则（多个分隔符用英文逗号分隔）
        /// </summary>
        public string Default_BlockRule
        {
            get => _defalut_blockRule;
            set { _defalut_blockRule = value; RaisePropertyChanged(); }
        }

        private int _defalut_SearchQuantity = 10;
        /// <summary>
        /// 全局分块规则（多个分隔符用英文逗号分隔）
        /// </summary>
        public int Default_SearchQuantity
        {
            get => _defalut_SearchQuantity;
            set { _defalut_SearchQuantity = value; RaisePropertyChanged(); }
        }

        private float _defalut_indexSimilarityThreshold = 0.6f;
        /// <summary>
        /// 全局分块规则（多个分隔符用英文逗号分隔）
        /// </summary>
        public float Default_IndexSimilarityThreshold
        {
            get => _defalut_indexSimilarityThreshold;
            set { _defalut_indexSimilarityThreshold = value; RaisePropertyChanged(); }
        }

        private string _fileBlockContent;
        /// <summary>
        /// 文件分块内容（用于预览）
        /// </summary>
        public string FileBlockContent
        {
            get => _fileBlockContent;
            set { _fileBlockContent = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<KnowledgeFileItem> _fileItems = new ObservableCollection<KnowledgeFileItem>();
        /// <summary>
        /// 知识库文件列表
        /// </summary>
        public ObservableCollection<KnowledgeFileItem> FileItems
        {
            get => _fileItems;
            set { _fileItems = value; RaisePropertyChanged(); }
        }



        private ObservableCollection<FileChunkItem> _fileBlocks = new ObservableCollection<FileChunkItem>();
        /// <summary>
        /// 当前查看的文件分块列表
        /// </summary>
        public ObservableCollection<FileChunkItem> FileBlocks
        {
            get => _fileBlocks;
            set { _fileBlocks = value; RaisePropertyChanged(); }
        }

        private string _currentFileName;
        /// <summary>
        /// 当前查看的文件名称
        /// </summary>
        public string CurrentFileName
        {
            get => _currentFileName;
            set { _currentFileName = value; RaisePropertyChanged(); }
        }

        private string _currentFileBlockRule;
        /// <summary>
        /// 当前查看文件的分块规则
        /// </summary>
        public string CurrentFileBlockRule
        {
            get => _currentFileBlockRule;
            set { _currentFileBlockRule = value; RaisePropertyChanged(); }
        }
    }

    /// <summary>
    /// 知识库文件项
    /// </summary>
    public class KnowledgeFileItem : ObservableObject
    {
        /// <summary>
        /// 文件完整路径
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 文件名（不含路径）
        /// </summary>
        public string FileName => System.IO.Path.GetFileName(FilePath);

        /// <summary>
        /// 分块数量
        /// </summary>
        public int ChunkCount { get; set; } = 0;

        private bool _isIndexed;
        /// <summary>
        /// 是否已索引
        /// </summary>
        public bool IsIndexed
        {
            get => _isIndexed;
            set { _isIndexed = value; RaisePropertyChanged(); }
        }

        private HashSet<int> _deletedChunkIndices = new HashSet<int>();
        /// <summary>
        /// 已删除的分块索引集合
        /// </summary>
        public HashSet<int> DeletedChunkIndices
        {
            get => _deletedChunkIndices;
            set { _deletedChunkIndices = value; RaisePropertyChanged(); }
        }

        private string _importBlockRule;
        /// <summary>
        /// 导入时使用的分块规则
        /// </summary>
        public string ImportBlockRule
        {
            get => _importBlockRule;
            set { _importBlockRule = value; RaisePropertyChanged(); }
        }
    }

    /// <summary>
    /// 文件分块项（用于显示）
    /// </summary>
    public class FileChunkItem : ObservableObject
    {
        private string _content;
        /// <summary>
        /// 分块文本内容
        /// </summary>
        public string Content
        {
            get => _content;
            set { _content = value; RaisePropertyChanged(); }
        }

        private int _originalIndex;
        /// <summary>
        /// 原始分块索引（0-based）
        /// </summary>
        public int OriginalIndex
        {
            get => _originalIndex;
            set { _originalIndex = value; RaisePropertyChanged(); }
        }

        public int EffectiveCharCount
        {
            get
            {
                if (string.IsNullOrEmpty(Content)) return 0;
                string s = Content.Replace("\r\n", "\n").Replace("\r", "\n");
                var lines = s.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                return lines.Sum(l => l.Length) + (lines.Length - 1) * 2;
            }

        }
    }
}