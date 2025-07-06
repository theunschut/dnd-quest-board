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
            .ReverseMap();

        // User mapping
        CreateMap<User, UserEntity>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) // Password is handled by Identity
            .ForMember(dest => dest.SecurityStamp, opt => opt.Ignore())
            .ForMember(dest => dest.ConcurrencyStamp, opt => opt.Ignore());

        CreateMap<UserEntity, User>()
            .ForMember(dest => dest.Password, opt => opt.Ignore()); // Don't map password back

        // PlayerSignup mapping
        CreateMap<PlayerSignup, PlayerSignupEntity>()
            .ForMember(dest => dest.Quest, opt => opt.Ignore())
            .ForMember(dest => dest.QuestId, opt => opt.MapFrom(src => src.Quest.Id))
            .ForMember(dest => dest.Player, opt => opt.Ignore())
            .ForMember(dest => dest.PlayerId, opt => opt.MapFrom(src => src.Player.Id));

        CreateMap<PlayerSignupEntity, PlayerSignup>();

        // ProposedDate mapping
        CreateMap<ProposedDate, ProposedDateEntity>()
            .ReverseMap();

        // PlayerDateVote mapping
        CreateMap<PlayerDateVote, PlayerDateVoteEntity>()
            .ForMember(dest => dest.Vote, opt => opt.MapFrom(src => src.Vote.HasValue ? (int)src.Vote.Value : (int?)null));

        CreateMap<PlayerDateVoteEntity, PlayerDateVote>()
            .ForMember(dest => dest.Vote, opt => opt.MapFrom(src => src.Vote.HasValue ? (VoteType)src.Vote.Value : (VoteType?)null));
    }
}