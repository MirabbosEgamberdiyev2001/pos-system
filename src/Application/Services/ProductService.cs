using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using POS.Application.Common.DataTransferObjects.ProductDtos;
using POS.Application.Common.Enums;
using POS.Application.Common.Exceptions;
using POS.Application.Common.Models;
using POS.Application.Common.Validators;
using POS.Application.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Interfaces;

namespace POS.Application.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProductService> _logger;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;

    private const string AllCacheKey     = "products:all";
    private const string ActiveCacheKey  = "products:active";
    private const string ArchiveCacheKey = "products:archive";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public ProductService(IUnitOfWork unitOfWork,
                          ILogger<ProductService> logger,
                          IMapper mapper,
                          IMemoryCache cache)
    {
        _unitOfWork = unitOfWork;
        _logger     = logger;
        _mapper     = mapper;
        _cache      = cache;
    }

    private void InvalidateCache()
    {
        _cache.Remove(AllCacheKey);
        _cache.Remove(ActiveCacheKey);
        _cache.Remove(ArchiveCacheKey);
    }

    public async Task ActionAsync(int id, ActionType action)
    {
        var model = await _unitOfWork.Products.GetByIdAsync(id)
                    ?? throw new ArgumentNullException(nameof(id), $"Mahsulot topilmadi: id={id}");

        switch (action)
        {
            case ActionType.Archive:
                model.IsDeleted = true;
                await _unitOfWork.Products.UpdateAsync(model);
                break;
            case ActionType.Recover:
                model.IsDeleted = false;
                await _unitOfWork.Products.UpdateAsync(model);
                break;
            case ActionType.Remove:
                await _unitOfWork.Products.RemoveAsync(model);
                break;
        }
        InvalidateCache();
    }

    public async Task<ProductDto> AddAsync(AddProductDto dto)
    {
        if (!dto.IsValid())
            throw new MarketException("Mahsulot nomini ko'rsating!");

        var products = await _unitOfWork.Products.GetAllAsync();
        if (products.Any(x => x.Name == dto.Name && x.IsEqual(dto)))
            throw new MarketException("Bu mahsulot omborda mavjud!");

        if (!string.IsNullOrEmpty(dto.Barcode) && products.Any(p => p.Barcode == dto.Barcode))
            throw new MarketException("Bu barcode avval ro'yxatga olingan!");

        var entity = _mapper.Map<Product>(dto);
        var saved  = await _unitOfWork.Products.AddAsync(entity);
        InvalidateCache();
        return _mapper.Map<ProductDto>(saved);
    }

    public async Task<string> GenerateBarcodeAsync()
    {
        // O(1) — DB'da unique tekshirish, barcha productlarni yuklamas
        string barcode;
        do
        {
            barcode = Random.Shared.NextInt64(1_000_000_000_000L, 9_999_999_999_999L).ToString();
        }
        while (await _unitOfWork.Products.AnyAsync(p => p.Barcode == barcode));
        return barcode;
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync()
    {
        if (_cache.TryGetValue(AllCacheKey, out IEnumerable<ProductDto>? cached) && cached != null)
            return cached;

        var list = await _unitOfWork.Products.GetAllWithCategories();
        var dtos = list.Select(p => _mapper.Map<ProductDto>(p)).ToList();
        _cache.Set(AllCacheKey, dtos, CacheTtl);
        return dtos;
    }

    public async Task<IEnumerable<ProductDto>> GetAllActivesAsync(int selectedCategoryId)
    {
        var all = await GetAllAsync();
        var filtered = all.Where(p => !p.IsDeleted);
        if (selectedCategoryId > 0)
            filtered = filtered.Where(p => p.Category?.Id == selectedCategoryId);
        return filtered.ToList();
    }

    public async Task<IEnumerable<ProductDto>> GetAllArchivesAsync(int selectedCategoryId)
    {
        var all = await GetAllAsync();
        var filtered = all.Where(p => p.IsDeleted);
        if (selectedCategoryId > 0)
            filtered = filtered.Where(p => p.Category?.Id == selectedCategoryId);
        return filtered.ToList();
    }

    public async Task<PagedList<ProductDto>> GetArchivedProductsAsync(int pageSize, int pageNumber)
    {
        var all = await GetAllAsync();
        var archived = all.Where(p => p.IsDeleted).ToList();
        var paged    = archived.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        return new PagedList<ProductDto>(paged, archived.Count, pageNumber, pageSize);
    }

    public async Task<ProductDto> GetByIdAsync(int id)
    {
        var entity = await _unitOfWork.Products.GetByIdAsync(id)
                     ?? throw new MarketException($"Mahsulot topilmadi: id={id}");
        return _mapper.Map<ProductDto>(entity);
    }

    public async Task<PagedList<ProductDto>> GetProductsAsync(int pageSize, int pageNumber)
    {
        var all    = await GetAllAsync();
        var active = all.Where(p => !p.IsDeleted).ToList();
        var paged  = active.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        return new PagedList<ProductDto>(paged, active.Count, pageNumber, pageSize);
    }

    public async Task<ProductDto> UpdateAsync(UpdateProductDto dto)
    {
        var entity = await _unitOfWork.Products.UpdateAsync(_mapper.Map<Product>(dto));
        InvalidateCache();
        return _mapper.Map<ProductDto>(entity);
    }

    public async Task<List<ProductDto>> FilterByNameAsync(string text, State state, int selectedCategoryId)
    {
        var list = state switch
        {
            State.Active  => await GetAllActivesAsync(selectedCategoryId),
            State.Archive => await GetAllArchivesAsync(selectedCategoryId),
            _             => await GetAllAsync()
        };
        return list.Where(x => x.Name.Contains(text, StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
