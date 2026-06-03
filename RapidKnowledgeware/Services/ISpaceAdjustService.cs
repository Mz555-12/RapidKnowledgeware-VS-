using RapidKnowledgeware.Models;

namespace RapidKnowledgeware.Services
{
    public interface ISpaceAdjustService
    {
        void ResetToDefault(SpaceAdjustModel target);
        void SaveAsDefault(SpaceAdjustModel source);
    }
}
