using System;
using System.Collections.Generic;

namespace Altostratus
{
    // These classes describe data as it comes from the backend.
    // TODO: replace with the same PCL that the backend uses and rename accordingly.
    public class FeedItem
    {
        public String Url { get; set; }
        public DateTime LastUpdated { get; set; }
        public String Title { get; set; }
        public String Body { get; set; }
        public String ProviderName { get; set; }
        public String CategoryName { get; set; }
    }


    public class RegisterExternalModel
    {
        public string Email { get; set; }
    }

    public class UserInfoViewModel
    {
        public string Email { get; set; }
        public bool HasRegistered { get; set; }
        public string LoginProvider { get; set; }
    }

    public class UserPreferenceDTO
    {
        public int ConversationLimit { get; set; }
        public int SortOrder { get; set; }
        public ICollection<String> Categories { get; set; }        
    }
}
