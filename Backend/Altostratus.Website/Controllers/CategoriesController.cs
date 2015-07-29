using Altostratus.ClientModels;
using Altostratus.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Altostratus.Website.Controllers
{
    public class CategoriesController : ApiController
    {
        private ApplicationDbContext db;

        public CategoriesController(ApplicationDbContext context)
        {
            db = context;
        }

        public IEnumerable<string> GetCategories()
        {
            return db.Categories.Select(x => x.Name);
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
