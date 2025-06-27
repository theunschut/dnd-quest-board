using AutoMapper;
using QuestBoard.Domain.Models;
using QuestBoard.Domain.Models.Users;
using QuestBoard.Service.ViewModels.DungeonMasterViewModels;
using QuestBoard.Service.ViewModels.QuestViewModels;

namespace QuestBoard.Service.Automapper;

public class ViewModelProfile : Profile
{
    public ViewModelProfile()
    {
        CreateMap<CreateDungeonMasterViewModel, DungeonMaster>();

        CreateMap<QuestViewModel, Quest>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.ProposedDates, opt => opt.MapFrom(src => src.ProposedDates))
            .ForMember(dest => dest.DungeonMaster, opt => opt.Ignore());

        CreateMap<DateTime, ProposedDate>()
            .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.QuestId, opt => opt.Ignore())
            .ForMember(dest => dest.Quest, opt => opt.Ignore());
    }
}