using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altostratus.DAL
{
    public class Category
    {
        public int CategoryID { get; set; }
        [StringLength(100)]
        public string Name { get; set; }  // e.g: Azure, ASP.NET

        public ICollection<Tag> Tags { get; set; }
        public ICollection<Conversation> Conversations { get; set; }
    }
}
