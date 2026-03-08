using AutoMapper;
using BgituGrades.Entities;
using BgituGrades.Models.Setting;
using BgituGrades.Repositories;

namespace BgituGrades.Services
{
    public interface ISettingService
    {
        public Task<SettingResponse> GetSettingsAsync();
        public Task UpdateSettingAsync(UpdateSettingRequest request);

    }
    public class SettingService(ISettingRepository settingRepository, IMapper mapper) : ISettingService
    {
        private readonly ISettingRepository _settingRepository = settingRepository;
        private readonly IMapper _mapper = mapper;
        public async Task<SettingResponse> GetSettingsAsync()
        {
            var calendarUrl = await _settingRepository.GetCalendarUrlAsync();
            var result = _mapper.Map<SettingResponse>(calendarUrl);
            return result;
        }

        public async Task UpdateSettingAsync(UpdateSettingRequest request)
        {
            var setting = _mapper.Map<Setting>(request);
            await _settingRepository.UpdateSettingAsync(setting);
        }
    }
}
