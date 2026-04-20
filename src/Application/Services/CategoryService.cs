using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using POS.Application.Common.DataTransferObjects.CategoryDtos;
using POS.Application.Common.Enums;
using POS.Application.Common.Exceptions;
using POS.Application.Common.Models;
using POS.Application.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Interfaces;

namespace POS.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;

    private const string AllCacheKey      = "categories:all";
    private const string ActiveCacheKey   = "categories:active";
    private const string ArchiveCacheKey  = "categories:archive";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public CategoryService(IUnitOfWork unitOfWork, IMapper mapper, IMemoryCache cache)
    {
        _unitOfWork = unitOfWork;
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
        var model = await _unitOfWork.Categories.GetByIdAsync(id)
                    ?? throw new MarketException($"Kategoriya topilmadi: id={id}");

        switch (action)
        {
            case ActionType.Archive:
                model.IsDeleted = true;
                await _unitOfWork.Categories.UpdateAsync(model);
                break;
            case ActionType.Recover:
                model.IsDeleted = false;
                await _unitOfWork.Categories.UpdateAsync(model);
                break;
            case ActionType.Remove:
                await _unitOfWork.Categories.RemoveAsync(model);
                break;
        }
        InvalidateCache();
    }

    public async Task<CategoryDto> AddAsync(AddCategoryDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new MarketException("Kategoriya nomi bo'sh bo'lmasligi kerak!");

        var all = await GetAllAsync();
        if (all.Any(x => x.Name.Equals(dto.Name, StringComparison.OrdinalIgnoreCase)))
            throw new MarketException("Bu kategoriya allaqachon mavjud!");

        var entity = _mapper.Map<Category>(dto);
        var saved  = await _unitOfWork.Categories.AddAsync(entity);
        InvalidateCache();
        return _mapper.Map<CategoryDto>(saved);
    }

    public async Task DeleteAsync(int id)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id)
                       ?? throw new MarketException($"Kategoriya topilmadi: id={id}");
        await _unitOfWork.Categories.RemoveAsync(category);
        InvalidateCache();
    }

    public void Dispose() => GC.SuppressFinalize(this);

    public async Task<List<CategoryDto>> FilterByNameAsync(string text, State state)
    {
        // DB-level search — EF.Functions.Like orqali (repository orqali)
        var all = state switch
        {
            State.Active  => await GetAllActivesAsync(),
            State.Archive => await GetAllArchivesAsync(),
            _             => await GetAllAsync()
        };
        return all.Where(x => x.Name.Contains(text, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task<List<CategoryDto>> GetAllActivesAsync()
    {
        if (_cache.TryGetValue(ActiveCacheKey, out List<CategoryDto>? cached) && cached != null)
            return cached;

        var list = await _unitOfWork.Categories.GetAllAsync();
        var dtos = list.Where(c => !c.IsDeleted).Select(c => _mapper.Map<CategoryDto>(c)).ToList();
        _cache.Set(ActiveCacheKey, dtos, CacheTtl);
        return dtos;
    }

    public async Task<List<CategoryDto>> GetAllArchivesAsync()
    {
        if (_cache.TryGetValue(ArchiveCacheKey, out List<CategoryDto>? cached) && cached != null)
            return cached;

        var list = await _unitOfWork.Categories.GetAllAsync();
        var dtos = list.Where(c => c.IsDeleted).Select(c => _mapper.Map<CategoryDto>(c)).ToList();
        _cache.Set(ArchiveCacheKey, dtos, CacheTtl);
        return dtos;
    }

    public async Task<List<CategoryDto>> GetAllAsync()
    {
        if (_cache.TryGetValue(AllCacheKey, out List<CategoryDto>? cached) && cached != null)
            return cached;

        var list = await _unitOfWork.Categories.GetAllAsync();
        var dtos = list.Select(c => _mapper.Map<CategoryDto>(c)).ToList();
        _cache.Set(AllCacheKey, dtos, CacheTtl);
        return dtos;
    }

    public async Task<PagedList<CategoryDto>> GetArchivedCategoriesAsync(int pageSize, int pageNumber)
    {
        var dtoList = await GetAllArchivesAsync();
        if (!dtoList.Any()) throw new MarketException("Arxivlangan kategoriyalar topilmadi");
        var paged = dtoList.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        return new PagedList<CategoryDto>(paged, dtoList.Count, pageNumber, pageSize);
    }

    public async Task<CategoryDto> GetByIdAsync(int id)
    {
        var entity = await _unitOfWork.Categories.GetByIdAsync(id)
                     ?? throw new MarketException($"Kategoriya topilmadi: id={id}");
        return _mapper.Map<CategoryDto>(entity);
    }

    public async Task<PagedList<CategoryDto>> GetCategoriesAsync(int pageSize, int pageNumber)
    {
        var dtoList = await GetAllActivesAsync();
        if (!dtoList.Any()) throw new MarketException("Aktiv kategoriyalar topilmadi");
        var sorted = dtoList.OrderByDescending(i => i.Id).ToList();
        var paged  = sorted.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        return new PagedList<CategoryDto>(paged, sorted.Count, pageNumber, pageSize);
    }

    public async Task<CategoryDto> UpdateAsync(UpdateCategoryDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new MarketException("Kategoriya nomi bo'sh bo'lmasligi kerak!");

        var all = await GetAllAsync();
        if (all.Any(x => x.Id != dto.Id && x.Name.Equals(dto.Name, StringComparison.OrdinalIgnoreCase)))
            throw new MarketException("Bu kategoriya allaqachon mavjud!");

        var model = await _unitOfWork.Categories.GetByIdAsync(dto.Id)
                    ?? throw new MarketException("Kategoriya topilmadi!");

        model.Name             = dto.Name;
        model.LastModifiedDate = DateTime.UtcNow;
        await _unitOfWork.Categories.UpdateAsync(model);
        InvalidateCache();
        return await GetByIdAsync(dto.Id);
    }
}
