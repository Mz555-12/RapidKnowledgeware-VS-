using RapidKnowledgeware.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RapidKnowledgeware.Functions.KnowledgeBaseFunc
{
    /// <summary>
    /// 知识库搜索服务：负责搜索逻辑、分页状态管理、结果集缓存
    /// </summary>
    public class KnowledgeBaseSearchService
    {
        private readonly KnowledgeBaseModel _model;
        private List<KnowledgeFileItem> _allFilteredItems = new List<KnowledgeFileItem>();

        /// <summary>
        /// 每页显示的条目数
        /// </summary>
        public const int PageSize = 9;

        /// <summary>
        /// 搜索关键词
        /// </summary>
        public string Keyword { get; set; } = string.Empty;

        /// <summary>
        /// 当前页码（从1开始）
        /// </summary>
        public int CurrentPage { get; set; } = 1;

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages => _allFilteredItems.Count == 0 ? 0 : (int)Math.Ceiling((double)_allFilteredItems.Count / PageSize);

        /// <summary>
        /// 是否有上一页
        /// </summary>
        public bool HasPreviousPage => CurrentPage > 1;

        /// <summary>
        /// 是否有下一页
        /// </summary>
        public bool HasNextPage => CurrentPage < TotalPages;

        /// <summary>
        /// 搜索结果是否为空
        /// </summary>
        public bool HasResults => _allFilteredItems.Count > 0;

        /// <summary>
        /// 初始化搜索服务
        /// </summary>
        /// <param name="model">知识库数据模型</param>
        public KnowledgeBaseSearchService(KnowledgeBaseModel model)
        {
            _model = model;
        }

        /// <summary>
        /// 执行搜索（模糊匹配文件名）。若关键词为空，则显示全部文件
        /// </summary>
        public void PerformSearch()
        {
            if (string.IsNullOrWhiteSpace(Keyword))
            {
                _allFilteredItems = _model.FileItems.ToList();
            }
            else
            {
                var keywordLower = Keyword.Trim().ToLowerInvariant();
                _allFilteredItems = _model.FileItems
                    .Where(f => f.FileName.ToLowerInvariant().Contains(keywordLower))
                    .ToList();
            }
            CurrentPage = 1;
        }

        /// <summary>
        /// 刷新搜索结果（当文件列表变化时调用，保持关键词不变）
        /// </summary>
        public void RefreshIfNeeded()
        {
            if (string.IsNullOrWhiteSpace(Keyword))
            {
                _allFilteredItems = _model.FileItems.ToList();
            }
            else
            {
                var keywordLower = Keyword.Trim().ToLowerInvariant();
                _allFilteredItems = _model.FileItems
                    .Where(f => f.FileName.ToLowerInvariant().Contains(keywordLower))
                    .ToList();
            }
            // 确保当前页不超过总页数
            if (CurrentPage > TotalPages)
                CurrentPage = TotalPages > 0 ? TotalPages : 1;
        }

        /// <summary>
        /// 获取当前页的条目集合
        /// </summary>
        public IEnumerable<KnowledgeFileItem> GetCurrentPageItems()
        {
            if (_allFilteredItems.Count == 0)
                return Enumerable.Empty<KnowledgeFileItem>();
            int startIndex = (CurrentPage - 1) * PageSize;
            return _allFilteredItems.Skip(startIndex).Take(PageSize);
        }
    }
}