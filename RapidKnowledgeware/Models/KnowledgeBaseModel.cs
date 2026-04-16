// KnowledgeBaseModel.cs
using GalaSoft.MvvmLight;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace RapidKnowledgeware.Models
{
    public class KnowledgeBaseModel : ObservableObject
    {
        private string _currentEmbeddingName = "bge-m3:567m";
        public string CurrentEmbeddingName
        {
            get => _currentEmbeddingName;
            set { _currentEmbeddingName = value; RaisePropertyChanged(); }
        }

        private string _blockRule = "###";
        public string BlockRule
        {
            get => _blockRule;
            set { _blockRule = value; RaisePropertyChanged(); }
        }

        private string _fileBlockContent;
        public string FileBlockContent
        {
            get => _fileBlockContent;
            set { _fileBlockContent = value; RaisePropertyChanged(); }
        }

        // 文件列表
        private ObservableCollection<KnowledgeFileItem> _fileItems = new ObservableCollection<KnowledgeFileItem>();
        public ObservableCollection<KnowledgeFileItem> FileItems
        {
            get => _fileItems;
            set { _fileItems = value; RaisePropertyChanged(); }
        }

        // 单例模式保持（若需要全局共享，已有Instance；但可能ViewModel中直接new Model即可，看你设计）
        private static KnowledgeBaseModel _instance;
        public static KnowledgeBaseModel Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new KnowledgeBaseModel();
                return _instance;
            }
        }

        private ObservableCollection<string> _fileBlocks = new ObservableCollection<string>();
        public ObservableCollection<string> FileBlocks
        {
            get => _fileBlocks;
            set { _fileBlocks = value; RaisePropertyChanged(); }
        }
    }

    public class KnowledgeFileItem : ObservableObject
    {
        public string FilePath { get; set; }
        public string FileName => System.IO.Path.GetFileName(FilePath);
        public int ChunkCount { get; set; } = 0;

        private bool _isIndexed;
        public bool IsIndexed
        {
            get => _isIndexed;
            set { _isIndexed = value; RaisePropertyChanged(); }
        }
    }
}