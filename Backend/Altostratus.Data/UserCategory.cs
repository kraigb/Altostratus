using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Altostratus.DAL
{
   public class UserCategory
   {
      public int UserCategoryId { get; set; }
      public string ApplicationUser_Id { get; set; }

      public int CategoryID { get; set; }

      public Category Category { get; set; }

      [ForeignKey("ApplicationUser_Id")]
      public ApplicationUser AppUser { get; set; }
   }
}
