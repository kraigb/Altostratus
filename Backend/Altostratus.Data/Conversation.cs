using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// You must manually fix the initial migrations and set the PK to non-clustered
//  .PrimaryKey(t => t.ConversationId, clustered: false) 
namespace Altostratus.DAL
{
    public class Conversation
    {
        public int ConversationId { get; set; }
        [StringLength(895), Required, Column(TypeName = "varchar")]
        [Index("UrlAndCategoryIdIndex", 1)]
        public string Url { get; set; }
        public DateTime LastUpdated { get; set; }         // This field is really LastUpdated, TODO change to LastUpdated
        public DateTimeOffset DbUpdated { get; set; }       // Client uses this to get all data newer than this date.
        [StringLength(500)]
        public string Title { get; set; }
        public string Body { get; set; }
        public int ProviderID { get; set; }           // SO or Twitter
        [Index("UrlAndCategoryIdIndex", 2, IsUnique=true)]
        public int CategoryID { get; set; }           // Azure, ASP.NET, Web Jobs

        public virtual Provider Provider { get; set; }
        public virtual Category category { get; set; }
    }
}
