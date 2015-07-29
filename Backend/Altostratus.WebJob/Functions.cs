using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Altostratus.DAL;
using Altostratus.Providers;
using System.Data.Entity;
using System.Configuration;

namespace Altostratus.WebJob
{
     
   public class Functions
   {
      private static ApplicationDbContext db = new ApplicationDbContext();

      [NoAutomaticTrigger]
      public static async Task GetThreadsAsync(string providerName, TextWriter logger)
      {
         try
         {
            var maxResults = int.Parse(ConfigurationManager.AppSettings[providerName + "MaxThreads"]);
            var fromDate = DateTime.Now.AddDays(int.Parse(ConfigurationManager.AppSettings[providerName + "HistoryMaxDays"]) * -1);
            var provider = await db.Providers.SingleAsync(p => p.Name == providerName);
            var categories = await db.Categories.Include(c => c.Tags).ToListAsync();

            IProviderAPI providerAPI = null;
            switch (providerName)
            {
               case "Twitter":
                  providerAPI = new TwitterProviderAPI();
                  break;
               case "StackOverflow":
                  providerAPI = new StackOverflowProviderAPI();
                  break;
            }

            logger.WriteLine("Getting {0} threads, max #={1}, earliest date={2}", providerName, maxResults, fromDate);
            await GetConversationsAsync(provider, providerAPI, categories, fromDate, maxResults, logger);
         }
         catch (Exception ex)
         {
            var message = string.Format("Exception calling {0} provider: {1}\n{2}", providerName, ex.Message, ex.StackTrace);
            // Write an error message to the WebJobs dashboard.
            logger.WriteLine(message);
            // Write an application error log.
            Console.Error.WriteLine(message);
            if (ex.InnerException != null) 
            {
               message = "InnerException: " + ex.InnerException;
               logger.WriteLine(message);
               Console.Error.WriteLine(message);
            }
             // Throw exception so Dashboard will show FAIL.
            throw;
         }
      }

      private static async Task GetConversationsAsync(Provider provider, IProviderAPI providerAPI, IEnumerable<Category> categories, DateTime fromDate, int maxResults, TextWriter logger)
      {
         // Loop through categories to make one provider API call for each category
         // that has tags for the current provider.
         foreach (Category category in categories)
         {
            IEnumerable<Tag> tagsForCategoryAndProvider = category.Tags.Where(t => t.ProviderID == provider.ProviderID);
            if (tagsForCategoryAndProvider.Count() > 0)
            {
               var newConversations = await providerAPI.GetConversationsAsync(provider, category, tagsForCategoryAndProvider, fromDate, maxResults, logger);
               await AddConversationsToDatabaseAsync(provider, category, newConversations, logger);
            }
            else
            {
                throw new Exception("DB not seeded; No tags for " + provider.Name.ToString());
            }
         }
      }

      private async static Task AddConversationsToDatabaseAsync(Provider provider, Category category, 
         IEnumerable<Conversation> newConversations, TextWriter logger)
      {
         int conversationsAddedToDatabase = 0;
         int conversationsUpdatedInDatabase = 0;

         foreach (var newConversation in newConversations)
         {
            var existingCon = db.Conversations
              .Where(c => c.CategoryID == category.CategoryID &&   
                          c.Url == newConversation.Url)            
               .SingleOrDefault();

            if (existingCon != null && existingCon.LastUpdated < newConversation.LastUpdated)
            {
               existingCon.LastUpdated = newConversation.LastUpdated;
               existingCon.DbUpdated = DateTimeOffset.UtcNow;
               existingCon.Body = newConversation.Body;
               db.Entry(existingCon).State = EntityState.Modified;
               conversationsUpdatedInDatabase++;
            }
            else if (existingCon == null)
            {
               newConversation.DbUpdated = DateTimeOffset.UtcNow;
               db.Conversations.Add(newConversation);
               conversationsAddedToDatabase++;
            }
         }
         logger.WriteLine("Added {0} new conversations, updated {1} conversations", 
            conversationsAddedToDatabase, conversationsUpdatedInDatabase);
         await db.SaveChangesAsync();
      }

      [NoAutomaticTrigger]
      public static async Task PurgeOldThreadsAsync(TextWriter logger)
      {
         try
         {
            var historyDays = int.Parse(ConfigurationManager.AppSettings["MaxDaysForPurge"]);
            var purgeDate = DateTime.Now.AddDays(historyDays * -1);

            var rowsAffected = await db.Database.ExecuteSqlCommandAsync(
                "DELETE FROM Conversations WHERE LastUpdated < {0}", purgeDate);

            logger.WriteLine("Purged {0} conversations earlier than {0}", rowsAffected, purgeDate);
         }
         catch (Exception ex)
         {
            var message = string.Format("Exception purging old history: {0}\n{1}", ex.Message, ex.StackTrace);
            // Write an error message to the WebJobs dashboard.
            logger.WriteLine(message);
            // Write an application error log.
            Console.Error.WriteLine(message);
            // Throw exception so Dashboard will show FAIL.
            throw;
         }
      }
   }
}
