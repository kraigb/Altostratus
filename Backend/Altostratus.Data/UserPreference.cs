using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Altostratus.DAL
{
   public class UserPreference
   {
      // FK to AspNetUser table Id    
      [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
      public string ApplicationUser_Id { get; set; }

      public int ConversationLimit { get; set; }     // 1 to 100
      public int SortOrder { get; set; }             // int mapping to relevance, newest first, oldest first.

      public ICollection<UserCategory> UserCategory { get; set; }

      [ForeignKey("ApplicationUser_Id")]
      public ApplicationUser AppUser { get; set; }
   }
}
