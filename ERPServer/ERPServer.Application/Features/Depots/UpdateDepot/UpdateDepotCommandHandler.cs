using AutoMapper;
using ERPServer.Domain.Entities;
using ERPServer.Domain.Repository;
using GenericRepository;
using MediatR;
using TS.Result;

namespace ERPServer.Application.Features.Depots.UpdateDepot;

internal sealed class UpdateDepotCommandHandler(
    IDepotRepository depotRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper
    ) : IRequestHandler<UpdateDepotCommand, Result<string>>
{
    public async Task<Result<string>> Handle(UpdateDepotCommand request, CancellationToken cancellationToken)
    {
        Depot? depot = await depotRepository.GetByExpressionWithTrackingAsync(p => p.Id == request.Id, cancellationToken);

        if (depot is null)
            return Result<string>.Failure("Depo bulunamadı!");

        bool isNameExists = await depotRepository.AnyAsync(p => p.Name.ToLower() == request.Name.ToLower(), cancellationToken);
        if (isNameExists)
            return Result<string>.Failure("Bu isimde bir depo zaten mevcut!");

        mapper.Map(request,depot);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return "Depo bilgileri başarıyla güncellendi!";
    }
}
