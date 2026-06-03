using RapidKnowledgeware.Models;
using System.Collections.Generic;

namespace RapidKnowledgeware.Services
{
    public interface IKnowledgeBaseSearchService
    {
        string Keyword { get; set; }
        int CurrentPage { get; set; }
        int TotalPages { get; }
        bool HasPreviousPage { get; }
        bool HasNextPage { get; }
        bool HasResults { get; }
        void PerformSearch();
        void RefreshIfNeeded();
        IEnumerable<KnowledgeFileItem> GetCurrentPageItems();
    }
}
