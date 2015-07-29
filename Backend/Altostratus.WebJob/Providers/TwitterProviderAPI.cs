using Altostratus.DAL;
using LinqToTwitter;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Altostratus.Providers
{
   class TwitterProviderAPI : IProviderAPI
   {
      public async Task<IEnumerable<DAL.Conversation>> GetConversationsAsync(Provider provider, Altostratus.DAL.Category category, IEnumerable<Tag> tags, DateTime from, int maxResults, TextWriter logger)
      {
         var conversations = new List<Altostratus.DAL.Conversation>();

         var twitterContext = new TwitterContext(getAuth());

         if (maxResults > 100)
         {
            logger.WriteLine("maxResults {0} > 100 ", maxResults);
            throw new Exception("maxResults " + maxResults + " > 100");
         }

         try
         {
             // inclusive OR clause
             // https://dev.twitter.com/rest/public/search
             var tagString = String.Join(" OR ", tags.Select(t => t.Value));

             var searchResponse = await
                       (from search in twitterContext.Search
                        where search.Type == SearchType.Search &&
                              search.Query == tagString &&
                              search.Count == maxResults
                        select search)
                       .SingleOrDefaultAsync();

             if (searchResponse == null || searchResponse.Statuses == null)
             {
                 string errMsg = searchResponse == null ? "null Response from LinqToTwitter" : "null Statuses from LinqToTwitter";
                 logger.WriteLine(errMsg);
                 return conversations;
             }

             logger.WriteLine("Found {0} tweets for tag {1}", searchResponse.Statuses.Count(), tagString);

             // Perform a LINQ to Objects query on the results filtering out retweets.

             var nonRetweetedStatuses =
                 (from tweet in searchResponse.Statuses
                  where tweet.RetweetedStatus.StatusID == 0
                  select tweet)
                 .ToList();

             if (nonRetweetedStatuses == null)
             {
                 logger.WriteLine("null Response from No RetweetedStatus on LinqToTwitter ");
                 return conversations;
             }

             logger.WriteLine("Found {0} tweets with nonRetweetedStatuses", nonRetweetedStatuses.Count.ToString());

             nonRetweetedStatuses.ForEach(tweet =>
                     conversations.Add(
                         new Altostratus.DAL.Conversation()
                         {
                             Body = ConvertURLsToLinks(tweet.Text),
                             ProviderID = provider.ProviderID,
                             Title = "Tweet by " + tweet.User.ScreenNameResponse,
                             LastUpdated = tweet.CreatedAt,
                             Url = "http://twitter.com/" + tweet.User.UserIDResponse + "/status/" + tweet.StatusID,
                             CategoryID = category.CategoryID
                         }));
         }

         catch (TwitterQueryException tqe)
         {
            logger.WriteLine(tqe.Message);
            // You can test this by  passing in a negative number for search
            // GetConversationsAsync(maxResults: -3);
            throw tqe;
         }
         return conversations;
      }

      SingleUserAuthorizer getAuth()
      {
         string consumerKey = ConfigurationManager.AppSettings["TwitterConsumerKey"];
         string consumerSecret = ConfigurationManager.AppSettings["TwitterSecret"];

         if (string.IsNullOrEmpty(consumerKey) || string.IsNullOrEmpty(consumerSecret))
         {
            throw new Exception("TwitterConsumerKey or TwitterSecret is null");
         }

         SingleUserAuthorizer authorizer = new SingleUserAuthorizer
         {
            CredentialStore = new InMemoryCredentialStore
            {
               // App only Auth uses these keys.
               ConsumerKey = consumerKey,
               ConsumerSecret = consumerSecret
            }
         };

         return authorizer;
      }

      private string ConvertURLsToLinks(string conversation)
      {
          string regex = @"((www\.|(http|https|ftp|news|file)+\:\/\/)[&#95;.a-z0-9-]+\.[a-z0-9\/&#95;:@=.+?,##%&~-]*[^.|\'|\# |!|\(|?|,| |>|<|;|\)])";
          Regex r = new Regex(regex, RegexOptions.IgnoreCase);
          var retVal = r.Replace(conversation, "<a href=\"$1\" target=\"&#95;blank\">$1</a>").Replace("href=\"www", "href=\"http://www");
          return retVal;
      }

   }
}
