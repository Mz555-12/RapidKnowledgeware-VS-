using RapidKnowledgeware.Models;

namespace RapidKnowledgeware.DAO
{
    public interface IAppSettingsRepository
    {
        void SaveSettings<T>(T model) where T : class;
        T LoadSettings<T>() where T : class;
        void LoadSettings<T>(T target) where T : class;
        void SaveSettings(KnowledgeBaseModel model);
        void LoadSettings(KnowledgeBaseModel model);
    }
}
