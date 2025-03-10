using AutoMapper;
using ERPServer.Application.Features.Customers.CreateCustomer;
using ERPServer.Application.Features.Customers.UpdateCustomer;
using ERPServer.Application.Features.Depots.CreateDepot;
using ERPServer.Application.Features.Depots.UpdateDepot;
using ERPServer.Application.Features.Products.CreateProduct;
using ERPServer.Application.Features.Products.UpdateProduct;
using ERPServer.Domain.Entities;
using ERPServer.Domain.Enums;
using Microsoft.Extensions.Options;

namespace ERPServer.Application.Mapping
{
    public sealed class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<CreateCustomerCommand,Customer>();
            CreateMap<UpdateCustomerCommand,Customer>();           
            
            CreateMap<CreateDepotCommand,Depot>();
            CreateMap<UpdateDepotCommand,Depot>();

            CreateMap<CreateProductCommand,Product>().ForMember(member => member.ProductType, options => {
                options.MapFrom(map => ProductTypeEnum.FromValue(map.ProductTypeValue));
            });

            CreateMap<UpdateProductCommand,Product>().ForMember(member => member.ProductType, options => {
                options.MapFrom(map => ProductTypeEnum.FromValue(map.ProductTypeValue));
            });
        }
    }
}
