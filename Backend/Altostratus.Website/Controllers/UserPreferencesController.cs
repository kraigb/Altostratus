using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Altostratus.DAL;
using System.Security.Claims;
using Microsoft.AspNet.Identity;
using Altostratus.ClientModels;

namespace Altostratus.Website.Controllers
{
    [Authorize]
    public class UserPreferencesController : ApiController
    {
        private ApplicationDbContext db;

        public UserPreferencesController(ApplicationDbContext context)
        {
            db = context;
        }

        // GET: api/UserPreferences
        [ResponseType(typeof(UserPreferenceDTO))]
        public async Task<IHttpActionResult> GetUserPreferences()
        {
            var userId = User.Identity.GetUserId();

            var prefs = await db.UserPreferences
                .Include(x => x.UserCategory.Select(y => y.Category))   // Multi-level include - http://romiller.com/2010/07/14/ef-ctp4-tips-tricks-include-with-lambda/
                .SingleOrDefaultAsync(x => x.ApplicationUser_Id == userId);

            if (prefs == null)
            {
                return NotFound();
            }
            else
            {
                var results = AutoMapper.Mapper.Map<UserPreference, UserPreferenceDTO>(prefs);
                return Ok(results);
            }
        }

        // PUT: api/UserPreferences
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutUserPreference(UserPreferenceDTO userPrefDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (userPrefDto == null)
            {
                return BadRequest("Null object");
            }
            if (userPrefDto.Categories == null)
            {
                return BadRequest();    // DataAnnotations is not supported in PLC :-(
            }

            var userId = User.Identity.GetUserId();

            var categories = db.Categories
                .Where(x => userPrefDto.Categories.Contains(x.Name))
                .ToList();

            if (categories.Count != userPrefDto.Categories.Count)
            {
                // Client sent a bad category name
                return BadRequest("Invalid category name");
            }

            UserPreference userPreference = new UserPreference
            {
                ApplicationUser_Id = userId,
                ConversationLimit = userPrefDto.ConversationLimit,
                SortOrder = userPrefDto.SortOrder,
                UserCategory = new List<UserCategory>()
            };

            if (UserPreferenceExists(userId))
            {
                db.UserPreferences.Attach(userPreference);
                db.Entry(userPreference).State = EntityState.Modified;
                db.Entry(userPreference).Collection(x => x.UserCategory).Load();

                userPreference.UserCategory.Clear();
                foreach (var c in categories)
                {
                    userPreference.UserCategory.Add(new UserCategory { Category = c, ApplicationUser_Id = userId });
                }
            }
            else
            {
                foreach (var c in categories)
                {
                    userPreference.UserCategory.Add(new UserCategory { Category = c, ApplicationUser_Id = userId });
                }
                db.UserPreferences.Add(userPreference);
            }

            await db.SaveChangesAsync();

            return StatusCode(HttpStatusCode.NoContent);
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool UserPreferenceExists(string id)
        {
            return db.UserPreferences.Any(e => e.ApplicationUser_Id == id);
        }
    }
}