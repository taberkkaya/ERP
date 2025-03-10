﻿using AutoMapper;
using ERPServer.Domain.Entities;
using ERPServer.Domain.Repository;
using GenericRepository;
using MediatR;
using TS.Result;

namespace ERPServer.Application.Features.Depots.CreateDepot;

internal sealed class CreateDepotCommandHandler(
    IDepotRepository depotRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper
    ) : IRequestHandler<CreateDepotCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CreateDepotCommand request, CancellationToken cancellationToken)
    {
        bool isNameExists = await depotRepository.AnyAsync(p => p.Name.ToLower() == request.Name.ToLower(),cancellationToken);
        if (isNameExists)
            return Result<string>.Failure("Bu isimde bir depo zaten mevcut!");

        Depot depot = mapper.Map<Depot>(request);

        await depotRepository.AddAsync(depot,cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return "Depo başarıyla kaydedildi.";
    }
}
