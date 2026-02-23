using AutoMapper;
using BgituGrades.Entities;
using BgituGrades.Models.Transfer;
using BgituGrades.Repositories;

namespace BgituGrades.Services
{
    public interface ITransferService
    {
        Task<IEnumerable<TransferResponse>> GetAllTransfersAsync();
        Task<TransferResponse> CreateTransferAsync(CreateTransferRequest request);
        Task<TransferResponse?> GetTransferByIdAsync(int id);
        Task<bool> UpdateTransferAsync(UpdateTransferRequest request);
        Task<bool> DeleteTransferAsync(int id);
    }
    public class TransferService(ITransferRepository transferRepository, IMapper mapper) : ITransferService
    {
        private readonly ITransferRepository _transferRepository = transferRepository;
        private readonly IMapper _mapper = mapper;

        public async Task<TransferResponse> CreateTransferAsync(CreateTransferRequest request)
        {
            var entity = _mapper.Map<Transfer>(request);
            var createdEntity = await _transferRepository.CreateTransferAsync(entity);
            return _mapper.Map<TransferResponse>(createdEntity);
        }

        public async Task<bool> DeleteTransferAsync(int id)
        {
            return await _transferRepository.DeleteTransferAsync(id);
        }

        public async Task<TransferResponse?> GetTransferByIdAsync(int id)
        {
            var entity = await _transferRepository.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<TransferResponse>(entity);
        }

        public async Task<IEnumerable<TransferResponse>> GetAllTransfersAsync()
        {
            var entities = await _transferRepository.GetAllTransfersAsync();
            return _mapper.Map<IEnumerable<TransferResponse>>(entities);
        }

        public async Task<bool> UpdateTransferAsync(UpdateTransferRequest request)
        {
            var entity = _mapper.Map<Transfer>(request);
            return await _transferRepository.UpdateTransferAsync(entity);
        }
    }
}
