using RapidKnowledgeware.Models;

namespace RapidKnowledgeware.Services
{
    public interface ILLMAdjustService
    {
        LLMAdjustModel Current { get; }
        void Save();
        SpaceAdjustModel CreateSpaceParametersFromDefault();
    }
}
