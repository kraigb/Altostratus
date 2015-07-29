using Altostratus.DAL;
using StackExchange.StacMan;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altostratus.Providers
{
   class StackOverflowProviderAPI : IProviderAPI
   {
      // Pass this as key when making requests against the Stack Exchange API to receive a higher request quota. This is not considered a secret, and may be safely embed in client side code or distributed binaries
      // TODO move key to app settings
      private StacManClient _client = new StacManClient(key: "oY9T1c4bbDhop6B64bjp1Q((", version: "2.1");

      public async Task<IEnumerable<Conversation>> GetConversationsAsync(Provider provider, Category category, IEnumerable<DAL.Tag> tags, DateTime fromDate, int maxResults, TextWriter logger)
      {
         var conversations = new List<Altostratus.DAL.Conversation>();

         // Create semi-colon delimited list of tags
         StringBuilder sb = new StringBuilder();
         foreach (var t in tags)
         {
            sb.AppendFormat("{0};", t.Value.ToLowerInvariant());
         }

         string tagString = sb.ToString();         
         ISearchMethods search = _client;

         try
         {
            var results = await search.GetMatches("stackoverflow",
                tagged: tagString,
                fromdate: fromDate,
                page: 1,
                pagesize: maxResults,
                filter: "!Ldk(uYFB4LapzC5C.3)FCD");  // Includes answer body and more

            if (results == null)
            {
               logger.WriteLine("StacMan call failed, null response", tagString);
               throw new Exception("StacMan call failed, null response");
            }

            if (results.Error != null)
            {
               logger.WriteLine("StacMan error: {0}", results.Error.Message);
               throw results.Error;
            }

            if (results.Data.Items.Length == 0)
            {
               logger.WriteLine("No threads found for tags {0}", tagString);
               return conversations;
            }

            if (results.Data.Backoff > 0)
            {
               logger.WriteLine("Throttling in effect for {0} seconds", results.Data.Backoff);
               // http://api.stackexchange.com/docs/throttle
               // In V2 we could stop this provider only and not delay other providers.
               await Task.Delay(1000 * (int)results.Data.Backoff);
            }

            logger.WriteLine("{0} threads found for tags {1}", results.Data.Items.Length, tagString);
            foreach (var question in results.Data.Items)
            {
               var sbq = new StringBuilder();
               sbq.Append(question.Body);
               var lastUpdate = question.LastActivityDate;

               if (question.AnswerCount > 0)
               {
                  foreach (var answer in question.Answers)
                  {
                      sbq.Append("<div class='panel panel-primary'><div class='panel-heading'><h3 class='panel-title'>Answer</h3></div><div class='panel-body'>" + answer.Body + "</div></div>");
                      if (answer.LastActivityDate > lastUpdate)
                     {
                        lastUpdate = answer.LastActivityDate;
                     }
                  }
               }
               conversations.Add(new Conversation
               {
                  Title = question.Title,
                  Body = sbq.ToString(),
                  Url = question.Link,
                  LastUpdated = lastUpdate,
                  ProviderID = provider.ProviderID,
                  CategoryID = category.CategoryID
               });
            }
         }
         catch (Exception e)
         {
            logger.WriteLine("Exception: {0}\n{1}", e.Message, e.StackTrace);
            throw;
         }
         return conversations;
      }
   }
}
