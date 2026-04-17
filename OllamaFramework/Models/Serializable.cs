using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OllamaFramework.Models
{
    public class Serializable
    {

        public class IndexData
        {
            public List<ChunkData> Chunks { get; set; }
        }

        public class ChunkData
        {
            public string Content { get; set; }
            public float[] Embedding { get; set; }
            public Dictionary<string, object> Metadata { get; set; }
        }

    }
}
