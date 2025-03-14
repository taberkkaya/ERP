﻿using AutoMapper;
using ERPServer.Domain.Entities;
using ERPServer.Domain.Repository;
using GenericRepository;
using MediatR;
using TS.Result;

namespace ERPServer.Application.Features.Customers.CreateCustomer;

public sealed class CreateCustomerCommandHandler(
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IRequestHandler<CreateCustomerCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        bool isTaxNumberExist = await customerRepository.AnyAsync(p => p.TaxNumber == request.TaxNumber, cancellationToken);
        if (isTaxNumberExist)
            return Result<string>.Failure("Vergi numarası daha önce kaydedilmiş!");

        Customer customer = mapper.Map<Customer>(request);

        await customerRepository.AddAsync(customer,cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return "Müşteri kaydı başarıyla tamamlandı.";
    }
}
