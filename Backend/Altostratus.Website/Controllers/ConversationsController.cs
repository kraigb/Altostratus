using Altostratus.ClientModels;
using Altostratus.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Data.Entity;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNet.Identity;


namespace Altostratus.Website.Controllers
{
    //    [RoutePrefix("api/conversations")]
    public class ConversationsController : ApiController
    {
        // For grouping conversations by category, before sending them over the wire
        class AggregatedConversations
        {
            public IEnumerable<Conversation> Children { get; set; }
        }

        const int DefaultConversationLimit = 100;

        private ApplicationDbContext db;

        public ConversationsController(ApplicationDbContext context)
        {
            db = context;

            db.Database.Log = x =>
            {
                System.Diagnostics.Debug.WriteLine(x);
            };
        }

        // GET: api/Conversations
        public async Task<IQueryable<ConversationDTO>> GetConversations(DateTimeOffset? from = null)
        {
            UserPreference prefs = null;
            ICollection<Category> categories = null;

            // Get user preferences
            if (User.Identity.IsAuthenticated)
            {
                string userId = User.Identity.GetUserId();

                prefs = await db.UserPreferences
                    .Include(x => x.UserCategory.Select(y => y.Category))
                    .Where(x => x.ApplicationUser_Id == userId).SingleOrDefaultAsync();
            }

            int maxPerCategory;

            // Preferences can be null if (a) request is not authenticated or (b) user never set any preferences.
            if (prefs != null)
            {
                categories = prefs.UserCategory.Select(u => u.Category).ToList();
                maxPerCategory = prefs.ConversationLimit;
            }
            else
            {
                maxPerCategory = DefaultConversationLimit;
            }

            // Add "from" clause if specified
            var q = db.Conversations as IQueryable<Conversation>;
            if (from != null)
            {
                q = q.Where(x => x.DbUpdated >= from.Value);
            }

            // The query to use depends on whether we have valid user preferences.
            // If not, we return all categories by default.
            IEnumerable<AggregatedConversations> results;
            if (categories == null)
            {
                results = (from conv in q
                           group conv by conv.CategoryID into g
                           select new AggregatedConversations
                           {
                               Children = (from c in g
                                           orderby c.LastUpdated descending
                                           select c).Take(maxPerCategory)

                           });
            }
            else
            {
                results = (from category in categories
                           join conv in q on category.CategoryID equals conv.CategoryID into g
                           select new AggregatedConversations
                           {
                               Children = (from c in g
                                           orderby c.LastUpdated descending
                                           select c).Take(maxPerCategory)
                           });
            }

            // Flatten grouped conversations into a list
            List<Conversation> convos = new List<Conversation>();
            foreach(var r in results)
            {
                convos.AddRange(r.Children);
            }

            return convos.AsQueryable().Project().To<ConversationDTO>();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
