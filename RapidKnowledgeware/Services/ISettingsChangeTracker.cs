namespace RapidKnowledgeware.Services
{
    public interface ISettingsChangeTracker
    {
        void CaptureSnapshot(object target);
        string GetChangesAndClear(object target);
    }
}
