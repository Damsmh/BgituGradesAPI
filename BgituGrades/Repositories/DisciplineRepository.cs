using BgituGrades.Data;
using BgituGrades.Entities;
using BgituGrades.Models.Discipline;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace BgituGrades.Repositories
{
    public interface IDisciplineRepository {
        Task<IEnumerable<Discipline>> GetAllAsync();
        Task<Discipline> CreateDisciplineAsync(Discipline entity);
        Task<Discipline?> GetByIdAsync(int id);
        Task<IEnumerable<Discipline?>> GetByGroupIdAsync(int groupId);
        Task<IEnumerable<Discipline?>> GetByGroupIdsAsync(int[] groupIds);
        Task<bool> UpdateDisciplineAsync(Discipline entity);
        Task<bool> DeleteDisciplineAsync(int id);
        Task DeleteAllAsync();
        Task<IEnumerable<Discipline>> GetDisciplinesByIdsAsync(int[] disciplineIds);
    }

    public class DisciplineRepository(IDbContextFactory<AppDbContext> contextFactory) : IDisciplineRepository
    {
        public async Task<Discipline> CreateDisciplineAsync(Discipline entity)
        {
            using var context = await contextFactory.CreateDbContextAsync();
            await context.Disciplines.AddAsync(entity);
            await context.SaveChangesAsync();  
            return entity;
        }

        public async Task DeleteAllAsync()
        {
            using var context = await contextFactory.CreateDbContextAsync();
            await context.Disciplines.ExecuteDeleteAsync();
        }

        public async Task<bool> DeleteDisciplineAsync(int id)
        {
            using var context = await contextFactory.CreateDbContextAsync();
            var entity = await GetByIdAsync(id);
            context.Disciplines.Remove(entity);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Discipline>> GetAllAsync()
        {
            using var context = await contextFactory.CreateDbContextAsync();
            return await context.Disciplines.AsNoTracking().ToListAsync();
        }

        public async Task<IEnumerable<Discipline?>> GetByGroupIdAsync(int groupId)
        {
            using var context = await contextFactory.CreateDbContextAsync();
            return await context.Disciplines
                .Where(d => d.Classes!.Any(c => c.GroupId == groupId))
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<Discipline?>> GetByGroupIdsAsync(int[] groupIds)
        {
            using var context = await contextFactory.CreateDbContextAsync();
            return await context.Disciplines
                .Where(d => d.Classes.Any(c => groupIds.Contains(c.GroupId)))
                .Distinct()
                .ToListAsync();
        }

        public async Task<Discipline?> GetByIdAsync(int id)
        {
            using var context = await contextFactory.CreateDbContextAsync();
            return await context.Disciplines.FindAsync(id);
        }

        public async Task<IEnumerable<Discipline>> GetDisciplinesByIdsAsync(int[] disciplineIds)
        {
            using var context = await contextFactory.CreateDbContextAsync();
            return await context.Disciplines
                .AsNoTracking()
                .Where(d => disciplineIds.Contains(d.Id))
                .ToListAsync();
        }

        public async Task<bool> UpdateDisciplineAsync(Discipline entity)
        {
            using var context = await contextFactory.CreateDbContextAsync();
            context.Disciplines.Update(entity);
            await context.SaveChangesAsync();
            return true;
        }
    }
}
