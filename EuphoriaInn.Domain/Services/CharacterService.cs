using AutoMapper;
using EuphoriaInn.Domain.Enums;
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Models;

namespace EuphoriaInn.Domain.Services;

internal class CharacterService(ICharacterRepository repository, IMapper mapper) : BaseService<Character>(repository, mapper), ICharacterService
{
    public async Task<IList<Character>> GetAllCharactersWithDetailsAsync(CancellationToken token = default)
    {
        return await repository.GetAllCharactersWithDetailsAsync(token);
    }

    public async Task<IList<Character>> GetCharactersByOwnerIdAsync(int ownerId, CancellationToken token = default)
    {
        return await repository.GetCharactersByOwnerIdAsync(ownerId, token);
    }

    public async Task<Character?> GetCharacterWithDetailsAsync(int id, CancellationToken token = default)
    {
        return await repository.GetCharacterWithDetailsAsync(id, token);
    }

    public async Task<Character?> GetMainCharacterForUserAsync(int userId, CancellationToken token = default)
    {
        return await repository.GetMainCharacterForUserAsync(userId, token);
    }

    public async Task SetAsMainCharacterAsync(int characterId, int userId, CancellationToken token = default)
    {
        // Get all user's characters
        var userCharacters = await repository.GetCharactersByOwnerIdAsync(userId, token);

        // Set all to backup
        foreach (var character in userCharacters)
        {
            character.Role = CharacterRole.Backup;
            await repository.UpdateAsync(character, token);
        }

        // Set the selected one to main
        var mainCharacter = userCharacters.FirstOrDefault(c => c.Id == characterId);
        if (mainCharacter != null)
        {
            mainCharacter.Role = CharacterRole.Main;
            await repository.UpdateAsync(mainCharacter, token);
        }
    }

    public Task<bool> ValidateCharacterClassLevelsAsync(int totalLevel, IList<CharacterClass> classes)
    {
        if (!classes.Any())
            return Task.FromResult(false);

        var sumOfClassLevels = classes.Sum(c => c.ClassLevel);
        return Task.FromResult(sumOfClassLevels == totalLevel);
    }

    public override async Task UpdateAsync(Character model, CancellationToken token = default)
    {
        await repository.UpdateProfileImageAsync(model.Id, model.ProfilePicture, token);
        await repository.UpdateAsync(model, token);
    }

    public Task<byte[]?> GetCharacterProfilePictureAsync(int id, CancellationToken token = default)
    {
        return repository.GetCharacterProfilePictureAsync(id, token);
    }
}
