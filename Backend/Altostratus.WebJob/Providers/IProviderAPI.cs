using Altostratus.DAL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altostratus.Providers
{
    interface IProviderAPI
    {
        // There's a Tags property on Category, but that collection includes tags for all providers, 
        // so a Tags collection is included separately here to avoid confusion.
        Task<IEnumerable<Conversation>> GetConversationsAsync(Provider provider, Category category, IEnumerable<Tag> tags, DateTime from, int maxResults, TextWriter logger);
    }
}
