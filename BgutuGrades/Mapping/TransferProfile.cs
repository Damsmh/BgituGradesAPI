using AutoMapper;
using BgutuGrades.Models.Transfer;
using Grades.Entities;

namespace BgutuGrades.Mapping
{
    public class TransferProfile : Profile
    {
        public TransferProfile()
        {
            CreateMap<CreateTransferRequest, Transfer>();
            CreateMap<UpdateTransferRequest, Transfer>();
            CreateMap<Transfer, TransferResponse>();
        }
    }
}
