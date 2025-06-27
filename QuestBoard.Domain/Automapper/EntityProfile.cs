using AutoMapper;
using QuestBoard.Domain.Enums;
using QuestBoard.Domain.Models;
using QuestBoard.Domain.Models.Users;
using QuestBoard.Repository.Entities;

namespace QuestBoard.Domain.Automapper;

public class EntityProfile : Profile
{
    public EntityProfile()
    {
        // Quest mapping
        CreateMap<Quest, QuestEntity>()
            .ForMember(dest => dest.Difficulty, opt => opt.MapFrom(src => (int)src.Difficulty));

        CreateMap<QuestEntity, Quest>()
            .ForMember(dest => dest.Difficulty, opt => opt.MapFrom(src => (Difficulty)src.Difficulty));

        // DungeonMaster mapping
        CreateMap<DungeonMaster, DungeonMasterEntity>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.ToLower()))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email != null ? src.Email.ToLower() : null));

        CreateMap<DungeonMasterEntity, DungeonMaster>();

        // PlayerSignup mapping
        CreateMap<PlayerSignup, PlayerSignupEntity>()
            .ReverseMap();

        // ProposedDate mapping
        CreateMap<ProposedDate, ProposedDateEntity>()
            .ReverseMap();

        // PlayerDateVote mapping
        CreateMap<PlayerDateVote, PlayerDateVoteEntity>()
            .ForMember(dest => dest.Vote, opt => opt.MapFrom(src => (int)src.Vote));

        CreateMap<PlayerDateVoteEntity, PlayerDateVote>()
            .ForMember(dest => dest.Vote, opt => opt.MapFrom(src => (VoteType)src.Vote));
    }
}