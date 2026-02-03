using EuphoriaInn.Repository.Entities;
using EuphoriaInn.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EuphoriaInn.Repository;

internal class CharacterRepository(QuestBoardContext context) : BaseRepository<CharacterEntity>(context), ICharacterRepository
{
    private readonly QuestBoardContext _context = context;

    public async Task<IList<CharacterEntity>> GetAllCharactersWithDetailsAsync(CancellationToken token = default)
    {
        return await _context.Characters
            .Include(c => c.Owner)
            .Include(c => c.Classes)
            .OrderByDescending(c => c.Status == 0) // 0 = Active
            .ThenBy(c => c.Owner.Name)
            .ThenBy(c => c.Name)
            .ToListAsync(token);
    }

    public async Task<IList<CharacterEntity>> GetCharactersByOwnerIdAsync(int ownerId, CancellationToken token = default)
    {
        return await _context.Characters
            .Include(c => c.Owner)
            .Include(c => c.Classes)
            .Where(c => c.OwnerId == ownerId)
            .OrderByDescending(c => c.Role == 0) // 0 = Main
            .ThenByDescending(c => c.Status == 0) // 0 = Active
            .ThenBy(c => c.Name)
            .ToListAsync(token);
    }

    public async Task<CharacterEntity?> GetCharacterWithDetailsAsync(int id, CancellationToken token = default)
    {
        return await _context.Characters
            .Include(c => c.Owner)
            .Include(c => c.Classes)
            .FirstOrDefaultAsync(c => c.Id == id, token);
    }

    public async Task<CharacterEntity?> GetMainCharacterForUserAsync(int userId, CancellationToken token = default)
    {
        return await _context.Characters
            .Include(c => c.Classes)
            .FirstOrDefaultAsync(c => c.OwnerId == userId && c.Role == 0, token); // 0 = Main
    }
}
