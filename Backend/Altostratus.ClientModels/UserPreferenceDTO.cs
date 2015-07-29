using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altostratus.ClientModels
{
    public class UserPreferenceDTO
    {
        public int ConversationLimit { get; set; }     // 1 to 100
        public int SortOrder { get; set; }             // int mapping to relevance, newest first, oldest first.
        public ICollection<string> Categories { get; set; }
    }
}
