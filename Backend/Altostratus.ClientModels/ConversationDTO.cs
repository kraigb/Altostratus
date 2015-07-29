using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altostratus.ClientModels
{
    public class ConversationDTO
    {
        public string Url { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string ProviderName { get; set; } // Automapper will automaticall map Provider.Name to this
        public string CategoryName { get; set; } // Automapper will automaticall map Category.Name to this
    }
}
