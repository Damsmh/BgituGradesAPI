using BgituGrades.Data;
using BgituGrades.Entities;
using Microsoft.EntityFrameworkCore;

namespace BgituGrades.Repositories
{
    public interface ITransferRepository
    {
        Task<IEnumerable<Transfer>> GetAllTransfersAsync();
        Task<Transfer> CreateTransferAsync(Transfer entity);
        Task<Transfer?> GetByIdAsync(int id);
        Task<bool> UpdateTransferAsync(Transfer entity);
        Task<bool> DeleteTransferAsync(int id);
        Task DeleteAllAsync();
    }

    public class TransferRepository(AppDbContext dbContext) : ITransferRepository
    {
        private readonly AppDbContext _dbContext = dbContext;

        public async Task<Transfer> CreateTransferAsync(Transfer entity)
        {
            await _dbContext.Transfers.AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteTransferAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            _dbContext.Transfers.Remove(entity);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<Transfer?> GetByIdAsync(int id)
        {
            var entity = await _dbContext.Transfers.FindAsync(id);
            return entity;
        }

        public async Task<IEnumerable<Transfer>> GetAllTransfersAsync()
        {
            var entities = await _dbContext.Transfers
                .AsNoTracking()
                .ToListAsync();
            return entities;
        }

        public async Task<bool> UpdateTransferAsync(Transfer entity)
        {
            _dbContext.Update(entity);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task DeleteAllAsync()
        {
            await _dbContext.Transfers.ExecuteDeleteAsync();
        }
    }

}
