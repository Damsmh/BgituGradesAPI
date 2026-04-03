using BgituGrades.Data;
using BgituGrades.Entities;
using Microsoft.EntityFrameworkCore;

namespace BgituGrades.Repositories
{
    public interface ITransferRepository
    {
        Task<IEnumerable<Transfer>> GetAllTransfersAsync(CancellationToken cancellationToken);
        Task<Transfer> CreateTransferAsync(Transfer entity, CancellationToken cancellationToken);
        Task<Transfer?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<bool> UpdateTransferAsync(Transfer entity, CancellationToken cancellationToken);
        Task<bool> DeleteTransferAsync(int id, CancellationToken cancellationToken);
        Task DeleteAllAsync(CancellationToken cancellationToken);
        Task<IEnumerable<Transfer>> GetTransfersByGroupAndDisciplineAsync(int groupId, int disciplineId, CancellationToken cancellationToken);
        Task<Dictionary<(int GroupId, int DisciplineId), IEnumerable<Transfer>>> GetTransfersByGroupIdsAsync(
            List<int> groupIds, CancellationToken cancellationToken);
    }

    public class TransferRepository(IDbContextFactory<AppDbContext> contextFactory) : ITransferRepository
    {
        public async Task<Transfer> CreateTransferAsync(Transfer entity, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            await context.Transfers.AddAsync(entity, cancellationToken: cancellationToken);
            await context.SaveChangesAsync(cancellationToken: cancellationToken);
            return entity;
        }

        public async Task<bool> DeleteTransferAsync(int id, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            var result = await context.Transfers
                .Where(t => t.Id == id)
                .ExecuteDeleteAsync(cancellationToken: cancellationToken);
            return result > 0;
        }

        public async Task<Transfer?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            var entity = await context.Transfers.FindAsync([id], cancellationToken: cancellationToken);
            return entity;
        }

        public async Task<IEnumerable<Transfer>> GetAllTransfersAsync(CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            var entities = await context.Transfers
                .AsNoTracking()
                .ToListAsync(cancellationToken: cancellationToken);
            return entities;
        }

        public async Task<bool> UpdateTransferAsync(Transfer entity, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            context.Update(entity);
            await context.SaveChangesAsync(cancellationToken: cancellationToken);
            return true;
        }

        public async Task DeleteAllAsync(CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            await context.Transfers.ExecuteDeleteAsync(cancellationToken: cancellationToken);
        }

        public async Task<IEnumerable<Transfer>> GetTransfersByGroupAndDisciplineAsync(int groupId, int disciplineId, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            var entities = await context.Transfers
                .Where(t => t.DisciplineId == disciplineId &&
                            t.GroupId == groupId)
                .AsNoTracking()
                .ToListAsync(cancellationToken: cancellationToken);
            return entities;
        }

        public async Task<Dictionary<(int GroupId, int DisciplineId), IEnumerable<Transfer>>> GetTransfersByGroupIdsAsync(
            List<int> groupIds, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            var transfers = await context.Transfers
                .Where(t => groupIds.Contains(t.GroupId))
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return transfers
                .GroupBy(t => (t.GroupId, t.DisciplineId))
                .ToDictionary(
                    g => g.Key,
                    g => g.AsEnumerable());
        }
    }

}
