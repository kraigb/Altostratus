using Altostratus.ClientModels;
using Altostratus.DAL;
using AutoMapper;
using System.Linq;

namespace Altostratus.Website
{
    public static class AutoMapperConfig
    {
        public static void Configure()
        {
            Mapper.CreateMap<Conversation, ConversationDTO>();

            Mapper.CreateMap<UserPreference, UserPreferenceDTO>()
                .ForMember(dest => dest.Categories,
                            opts => opts.MapFrom(src => src.UserCategory.Select(x => x.Category.Name).ToList()));
                // This clause maps ICollection<UserCategory> to a flat list of category names.
        }
    }
}
