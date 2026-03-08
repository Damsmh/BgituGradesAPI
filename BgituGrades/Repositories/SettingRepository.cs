using BgituGrades.Data;
using BgituGrades.Entities;
using Microsoft.EntityFrameworkCore;

namespace BgituGrades.Repositories
{
    public interface ISettingRepository
    {
        Task<Setting?> GetCalendarUrlAsync();
        Task UpdateSettingAsync(Setting setting);
    }
    public class SettingRepository(AppDbContext dbContext) : ISettingRepository
    {
        private readonly AppDbContext _dbContext = dbContext;
        public async Task<Setting?> GetCalendarUrlAsync()
        {
            var url = await _dbContext.Settings
                .AsNoTracking()
                .FirstOrDefaultAsync();
            return url;
        }

        public async Task UpdateSettingAsync(Setting setting)
        {
            var existing = await _dbContext.Settings.FirstOrDefaultAsync();

            if (existing is null)
                _dbContext.Settings.Add(setting);
            else
            {
                existing.CalendarUrl = setting.CalendarUrl;
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
