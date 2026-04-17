// KnowledgeBaseModel.cs
using GalaSoft.MvvmLight;
using System.Collections.Generic;
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

        private ObservableCollection<FileChunkItem> _fileBlocks = new ObservableCollection<FileChunkItem>();
        public ObservableCollection<FileChunkItem> FileBlocks
        {
            get => _fileBlocks;
            set { _fileBlocks = value; RaisePropertyChanged(); }
        }


        private string _currentFileName;
        public string CurrentFileName
        {
            get => _currentFileName;
            set { _currentFileName = value; RaisePropertyChanged(); }
        }

        private string _currentFileBlockRule;
        public string CurrentFileBlockRule
        {
            get => _currentFileBlockRule;
            set { _currentFileBlockRule = value; RaisePropertyChanged(); }
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

        private HashSet<int> _deletedChunkIndices = new HashSet<int>();
        public HashSet<int> DeletedChunkIndices
        {
            get => _deletedChunkIndices;
            set { _deletedChunkIndices = value; RaisePropertyChanged(); }
        }


        private string _importBlockRule;
        public string ImportBlockRule
        {
            get => _importBlockRule;
            set { _importBlockRule = value; RaisePropertyChanged(); }
        }
    }

    public class FileChunkItem : ObservableObject
    {
        private string _content;
        public string Content
        {
            get => _content;
            set { _content = value; RaisePropertyChanged(); }
        }

        private int _originalIndex;
        public int OriginalIndex
        {
            get => _originalIndex;
            set { _originalIndex = value; RaisePropertyChanged(); }
        }
    }
}