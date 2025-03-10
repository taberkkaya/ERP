using AutoMapper;
using ERPServer.Domain.Entities;
using ERPServer.Domain.Repository;
using GenericRepository;
using MediatR;
using TS.Result;

namespace ERPServer.Application.Features.Products.UpdateProduct;

internal sealed class UpdateProductHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IRequestHandler<UpdateProductCommand, Result<string>>
{
    public async Task<Result<string>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        Product? product = await productRepository.GetByExpressionWithTrackingAsync(p => p.Id == request.Id, cancellationToken);

        if (product is null)
            return Result<string>.Failure("Ürün bulunamadı!");

        bool isNameExist = await productRepository.AnyAsync(p => p.Name.ToLower() == request.Name.ToLower() && p.Name != request.Name, cancellationToken);
        if (isNameExist)
            return Result<string>.Failure("Bu isimde bir ürün zaten mevcut!");

        mapper.Map(request, product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return "Ürün başarıyla güncellendi.";
    }
}

