using AutoMapper;
using EuphoriaInn.Domain.Enums;
using EuphoriaInn.Domain.Models;
using EuphoriaInn.Domain.Models.QuestBoard;
using EuphoriaInn.Domain.Models.Shop;
using EuphoriaInn.Repository.Entities;

namespace EuphoriaInn.Domain.Automapper;

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
            .ForMember(dest => dest.PlayerId, opt => opt.MapFrom(src => src.Player.Id))
            .ForMember(dest => dest.SignupRole, opt => opt.MapFrom(src => (int)src.Role));

        CreateMap<PlayerSignupEntity, PlayerSignup>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => (SignupRole)src.SignupRole));

        // ProposedDate mapping
        CreateMap<ProposedDate, ProposedDateEntity>()
            .ReverseMap();

        // PlayerDateVote mapping
        CreateMap<PlayerDateVote, PlayerDateVoteEntity>()
            .ForMember(dest => dest.Vote, opt => opt.MapFrom(src => src.Vote.HasValue ? (int)src.Vote.Value : (int?)null));

        CreateMap<PlayerDateVoteEntity, PlayerDateVote>()
            .ForMember(dest => dest.Vote, opt => opt.MapFrom(src => src.Vote.HasValue ? (VoteType)src.Vote.Value : (VoteType?)null));

        // Shop entity mappings
        
        // ShopItem mapping with enum conversions
        CreateMap<ShopItem, ShopItemEntity>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => (int)src.Type))
            .ForMember(dest => dest.Rarity, opt => opt.MapFrom(src => (int)src.Rarity))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => (int)src.Status));

        CreateMap<ShopItemEntity, ShopItem>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => (ItemType)src.Type))
            .ForMember(dest => dest.Rarity, opt => opt.MapFrom(src => (ItemRarity)src.Rarity))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => (ItemStatus)src.Status));


        // UserTransaction mapping
        CreateMap<UserTransaction, UserTransactionEntity>()
            .ForMember(dest => dest.TransactionType, opt => opt.MapFrom(src => (int)src.TransactionType));

        CreateMap<UserTransactionEntity, UserTransaction>()
            .ForMember(dest => dest.TransactionType, opt => opt.MapFrom(src => (TransactionType)src.TransactionType));
    }
}