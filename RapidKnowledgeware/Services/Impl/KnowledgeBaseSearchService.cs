using IOC.Annotations;
using RapidKnowledgeware.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RapidKnowledgeware.Services.Impl
{
    [Service]
    public class KnowledgeBaseSearchService : IKnowledgeBaseSearchService
    {
        private List<KnowledgeFileItem> _allFilteredItems = new List<KnowledgeFileItem>();

        public const int PageSize = 8;

        public string Keyword { get; set; } = string.Empty;

        public int CurrentPage { get; set; } = 1;

        public int TotalPages => _allFilteredItems.Count == 0 ? 0 : (int)Math.Ceiling((double)_allFilteredItems.Count / PageSize);

        public bool HasPreviousPage => CurrentPage > 1;

        public bool HasNextPage => CurrentPage < TotalPages;

        public bool HasResults => _allFilteredItems.Count > 0;

        public KnowledgeBaseSearchService()
        {
        }

        public void PerformSearch()
        {
            if (string.IsNullOrWhiteSpace(Keyword))
            {
                _allFilteredItems = KnowledgeBaseModel.Instance.FileItems.ToList();
            }
            else
            {
                var keywordLower = Keyword.Trim().ToLowerInvariant();
                _allFilteredItems = KnowledgeBaseModel.Instance.FileItems
                    .Where(f => f.FileName.ToLowerInvariant().Contains(keywordLower))
                    .ToList();
            }
            CurrentPage = 1;
        }

        public void RefreshIfNeeded()
        {
            if (string.IsNullOrWhiteSpace(Keyword))
            {
                _allFilteredItems = KnowledgeBaseModel.Instance.FileItems.ToList();
            }
            else
            {
                var keywordLower = Keyword.Trim().ToLowerInvariant();
                _allFilteredItems = KnowledgeBaseModel.Instance.FileItems
                    .Where(f => f.FileName.ToLowerInvariant().Contains(keywordLower))
                    .ToList();
            }
            if (CurrentPage > TotalPages)
                CurrentPage = TotalPages > 0 ? TotalPages : 1;
        }

        public IEnumerable<KnowledgeFileItem> GetCurrentPageItems()
        {
            if (_allFilteredItems.Count == 0)
                return Enumerable.Empty<KnowledgeFileItem>();
            int startIndex = (CurrentPage - 1) * PageSize;
            return _allFilteredItems.Skip(startIndex).Take(PageSize);
        }
    }
}
