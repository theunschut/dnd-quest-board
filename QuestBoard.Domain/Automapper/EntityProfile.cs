using AutoMapper;
using QuestBoard.Domain.Enums;
using QuestBoard.Domain.Models;
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

        // User mapping
        CreateMap<User, UserEntity>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.IsDungeonMaster, opt => opt.MapFrom(src => src.IsDungeonMaster))
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) // Password is handled by Identity
            .ForMember(dest => dest.SecurityStamp, opt => opt.Ignore())
            .ForMember(dest => dest.ConcurrencyStamp, opt => opt.Ignore());

        CreateMap<UserEntity, User>()
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.IsDungeonMaster, opt => opt.MapFrom(src => src.IsDungeonMaster))
            .ForMember(dest => dest.Password, opt => opt.Ignore()); // Don't map password back

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