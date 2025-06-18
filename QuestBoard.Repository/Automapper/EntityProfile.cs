using AutoMapper;
using QuestBoard.Domain.Enums;
using QuestBoard.Domain.Models;
using QuestBoard.Repository.Entities;

namespace QuestBoard.Repository.Automapper;

public class EntityProfile : Profile
{
    public EntityProfile()
    {
        CreateMap<Quest, QuestEntity>()
            .ForMember(dest => dest.Difficulty, opt => opt.MapFrom(src => (int)src.Difficulty))
            .ForMember(dest => dest.DmEmail, opt => opt.MapFrom(src => src.DmEmail != null ? src.DmEmail.ToLower(): null))
            .ForMember(dest => dest.DmName, opt => opt.MapFrom(src => src.DmName.ToLower()));

        CreateMap<QuestEntity, Quest>()
            .ForMember(dest => dest.Difficulty, opt => opt.MapFrom(src => (Difficulty)src.Difficulty));

        CreateMap<PlayerSignup, PlayerSignupEntity>()
            .ReverseMap();

        CreateMap<ProposedDate, ProposedDateEntity>()
            .ReverseMap();

        CreateMap<PlayerDateVote, PlayerDateVoteEntity>()
            .ForMember(dest => dest.Vote, opt => opt.MapFrom(src => (int)src.Vote));

        CreateMap<PlayerDateVoteEntity, PlayerDateVote>()
            .ForMember(dest => dest.Vote, opt => opt.MapFrom(src => (VoteType)src.Vote));
    }
}