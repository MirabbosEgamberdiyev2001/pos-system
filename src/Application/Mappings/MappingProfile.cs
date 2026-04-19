using AutoMapper;
using POS.Application.Common.DataTransferObjects.CategoryDtos;
using POS.Application.Common.DataTransferObjects.ProductDtos;
using POS.Application.Common.DataTransferObjects.ReceiptDtos;
using POS.Application.Common.DataTransferObjects.TransactionDtos;
using POS.Application.Common.DataTransferObjects.WarehouseItemDtos;
using POS.Domain.Entities;
using POS.Domain.Entities.Selling;

namespace POS.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Category
        CreateMap<Category, CategoryDto>();
        CreateMap<AddCategoryDto, Category>()
            .ForMember(d => d.LastModifiedDate, opt => opt.MapFrom(_ => DateTime.UtcNow));

        // Product
        CreateMap<Product, ProductDto>()
            .ForMember(d => d.Category, opt => opt.MapFrom(s => s.Category));
        CreateMap<AddProductDto, Product>()
            .ForMember(d => d.Amount, opt => opt.MapFrom(_ => 0m))
            .ForMember(d => d.LastModifiedDate, opt => opt.MapFrom(_ => DateTime.UtcNow));
        CreateMap<UpdateProductDto, Product>()
            .ForMember(d => d.LastModifiedDate, opt => opt.MapFrom(_ => DateTime.UtcNow));

        // ProductItem
        CreateMap<ProductItem, ProductItemDto>()
            .ForMember(d => d.ProductName, opt => opt.MapFrom(s => s.Product != null ? s.Product.Name : string.Empty));
        CreateMap<AddProductItemDto, ProductItem>()
            .ForMember(d => d.AddedDate, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(d => d.LastModifiedDate, opt => opt.MapFrom(_ => DateTime.UtcNow));

        // Receipt
        CreateMap<Receipt, ReceiptDto>()
            .ForMember(d => d.SellerName, opt => opt.MapFrom(s =>
                s.Seller != null ? $"{s.Seller.FirstName} {s.Seller.LastName}" : string.Empty))
            .ForMember(d => d.Date, opt => opt.MapFrom(s => s.LastModifiedDate));
        CreateMap<AddReceiptDto, Receipt>()
            .ForMember(d => d.IsDeleted, opt => opt.MapFrom(_ => false))
            .ForMember(d => d.LastModifiedDate, opt => opt.MapFrom(_ => DateTime.UtcNow));

        // Transaction
        CreateMap<Transaction, TransactionDto>();
    }
}
