using Microsoft.Extensions.Logging;
using POS.Application.Common.DataTransferObjects.WarehouseItemDtos;
using POS.Application.Common.Enums;
using POS.Application.Common.Exceptions;
using POS.Application.Common.Models;
using POS.Application.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Interfaces;

namespace POS.Application.Services;

public class ProductItemService : IProductItemService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProductItemService> _logger;

    public ProductItemService(IUnitOfWork unitOfWork, ILogger<ProductItemService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ProductItemDto> AddAsync(AddProductItemDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));
        if (dto.BuyingPrice <= 0 || dto.SellingPrice <= 0 || dto.Amount <= 0)
            throw new MarketException("Narx va miqdor musbat son bo'lishi kerak");
        if (dto.SellingPrice < dto.BuyingPrice)
            throw new MarketException("Sotuv narxi xarid narxidan past bo'lishi mumkin emas");

        var product = await _unitOfWork.Products.GetByIdAsync(dto.ProductId);
        if (product == null)
            throw new MarketException("Mahsulot topilmadi");

        var model = await _unitOfWork.ProductItems.AddAsync((ProductItem)dto);

        // Mahsulot umumiy miqdorini yangilash
        var allItems = (await _unitOfWork.ProductItems.GetAllAsync())
            .Where(i => !i.IsDeleted && i.ProductId == dto.ProductId);
        product.Amount = allItems.Sum(p => p.Amount);
        await _unitOfWork.Products.UpdateAsync(product);

        _logger.LogInformation("ProductItem qo'shildi: ProductId={ProductId}, Amount={Amount}", dto.ProductId, dto.Amount);

        var saved = await _unitOfWork.ProductItems.GetByIdWithProductAsync(model.Id);
        return (ProductItemDto)(saved ?? model);
    }

    public async Task<List<ProductItemDto>> GetAllAsync()
    {
        var list = await _unitOfWork.ProductItems.GetAllWithProductAsync();
        return list.Select(i => (ProductItemDto)i).ToList();
    }

    public async Task<ProductItemDto> GetByIdAsync(int id)
    {
        var item = await _unitOfWork.ProductItems.GetByIdWithProductAsync(id);
        if (item == null) throw new MarketException($"ProductItem topilmadi: id={id}");
        return (ProductItemDto)item;
    }

    public async Task<PagedList<ProductItemDto>> GetPagedAsync(int pageSize, int pageNumber, int productId)
    {
        var all = await _unitOfWork.ProductItems.GetAllWithProductAsync();
        var filtered = all.Where(i => !i.IsDeleted);

        if (productId > 0)
            filtered = filtered.Where(i => i.ProductId == productId);

        var dtos = filtered.Select(i => (ProductItemDto)i).ToList();
        var paged = dtos.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        return new PagedList<ProductItemDto>(paged, dtos.Count, pageNumber, pageSize);
    }

    public async Task<PagedList<ProductItemDto>> GetArchivedAsync(int pageSize, int pageNumber)
    {
        var all = await _unitOfWork.ProductItems.GetAllWithProductAsync();
        var archived = all.Where(i => i.IsDeleted).Select(i => (ProductItemDto)i).ToList();
        var paged = archived.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        return new PagedList<ProductItemDto>(paged, archived.Count, pageNumber, pageSize);
    }

    public async Task<ProductItemDto> Update(UpdateWarehouseItemDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var existing = await _unitOfWork.ProductItems.GetByIdAsync(dto.Id);
        if (existing == null) throw new MarketException($"ProductItem topilmadi: id={dto.Id}");

        var updated = await _unitOfWork.ProductItems.UpdateAsync((ProductItem)dto);

        // Product umumiy miqdorini qayta hisoblash
        var product = await _unitOfWork.Products.GetByIdAsync(dto.ProductId);
        if (product != null)
        {
            var allItems = (await _unitOfWork.ProductItems.GetAllAsync())
                .Where(i => !i.IsDeleted && i.ProductId == dto.ProductId);
            product.Amount = allItems.Sum(p => p.Amount);
            await _unitOfWork.Products.UpdateAsync(product);
        }

        _logger.LogInformation("ProductItem yangilandi: Id={Id}", dto.Id);
        return (ProductItemDto)updated;
    }

    public async Task ActionAsync(int id, ActionType action)
    {
        var model = await _unitOfWork.ProductItems.GetByIdAsync(id);
        if (model == null) throw new ArgumentNullException(nameof(model), $"ProductItem topilmadi: id={id}");

        switch (action)
        {
            case ActionType.Archive:
                model.IsDeleted = true;
                await _unitOfWork.ProductItems.UpdateAsync(model);
                break;
            case ActionType.Recover:
                model.IsDeleted = false;
                await _unitOfWork.ProductItems.UpdateAsync(model);
                break;
            case ActionType.Remove:
                await _unitOfWork.ProductItems.RemoveAsync(model);
                break;
        }

        // Product umumiy miqdorini qayta hisoblash
        var product = await _unitOfWork.Products.GetByIdAsync(model.ProductId);
        if (product != null)
        {
            var allItems = (await _unitOfWork.ProductItems.GetAllAsync())
                .Where(i => !i.IsDeleted && i.ProductId == model.ProductId);
            product.Amount = allItems.Sum(p => p.Amount);
            await _unitOfWork.Products.UpdateAsync(product);
        }

        _logger.LogInformation("ProductItem action: Id={Id}, Action={Action}", id, action);
    }
}
