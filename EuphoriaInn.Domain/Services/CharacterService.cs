using AutoMapper;
using EuphoriaInn.Domain.Enums;
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Models;
using EuphoriaInn.Repository.Entities;
using EuphoriaInn.Repository.Interfaces;

namespace EuphoriaInn.Domain.Services;

internal class CharacterService(ICharacterRepository repository, IMapper mapper) : BaseService<Character, CharacterEntity>(repository, mapper), ICharacterService
{
    private readonly ICharacterRepository _repository = repository;

    public async Task<IList<Character>> GetAllCharactersWithDetailsAsync(CancellationToken token = default)
    {
        var entities = await _repository.GetAllCharactersWithDetailsAsync(token);
        return Mapper.Map<IList<Character>>(entities);
    }

    public async Task<IList<Character>> GetCharactersByOwnerIdAsync(int ownerId, CancellationToken token = default)
    {
        var entities = await _repository.GetCharactersByOwnerIdAsync(ownerId, token);
        return Mapper.Map<IList<Character>>(entities);
    }

    public async Task<Character?> GetCharacterWithDetailsAsync(int id, CancellationToken token = default)
    {
        var entity = await _repository.GetCharacterWithDetailsAsync(id, token);
        return entity == null ? null : Mapper.Map<Character>(entity);
    }

    public async Task<Character?> GetMainCharacterForUserAsync(int userId, CancellationToken token = default)
    {
        var entity = await _repository.GetMainCharacterForUserAsync(userId, token);
        return entity == null ? null : Mapper.Map<Character>(entity);
    }

    public async Task SetAsMainCharacterAsync(int characterId, int userId, CancellationToken token = default)
    {
        // Get all user's characters
        var userCharacters = await _repository.GetCharactersByOwnerIdAsync(userId, token);

        // Set all to backup
        foreach (var character in userCharacters)
        {
            character.Role = (int)CharacterRole.Backup;
        }

        // Set the selected one to main
        var mainCharacter = userCharacters.FirstOrDefault(c => c.Id == characterId);
        if (mainCharacter != null)
        {
            mainCharacter.Role = (int)CharacterRole.Main;
        }

        await _repository.SaveChangesAsync(token);
    }

    public Task<bool> ValidateCharacterClassLevelsAsync(int totalLevel, IList<CharacterClass> classes)
    {
        if (!classes.Any())
            return Task.FromResult(false);

        var sumOfClassLevels = classes.Sum(c => c.ClassLevel);
        return Task.FromResult(sumOfClassLevels == totalLevel);
    }
}
