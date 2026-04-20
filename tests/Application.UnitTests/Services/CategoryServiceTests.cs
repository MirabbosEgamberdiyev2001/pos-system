using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using NUnit.Framework;
using POS.Application.Common.DataTransferObjects.CategoryDtos;
using POS.Application.Common.Enums;
using POS.Application.Common.Models;
using POS.Application.Common.Exceptions;
using POS.Application.Mappings;
using POS.Application.Services;
using POS.Domain.Entities;
using POS.Domain.Interfaces;

namespace POS.Application.UnitTests.Services;

[TestFixture]
public class CategoryServiceTests
{
    private Mock<IUnitOfWork>      _unitOfWorkMock;
    private Mock<ICategoryInterface> _categoryRepoMock;
    private IMapper   _mapper;
    private IMemoryCache _cache;
    private CategoryService _service;

    [SetUp]
    public void SetUp()
    {
        _unitOfWorkMock   = new Mock<IUnitOfWork>();
        _categoryRepoMock = new Mock<ICategoryInterface>();
        _unitOfWorkMock.Setup(u => u.Categories).Returns(_categoryRepoMock.Object);

        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
        _cache  = new MemoryCache(new MemoryCacheOptions());

        _service = new CategoryService(_unitOfWorkMock.Object, _mapper, _cache);
    }

    [TearDown]
    public void TearDown() => _cache.Dispose();

    // ─── AddAsync ────────────────────────────────────────────────────────

    [Test]
    public async Task AddAsync_ValidDto_ReturnsCreatedCategory()
    {
        var dto    = new AddCategoryDto { Name = "Elektronika" };
        var entity = new Category { Id = 1, Name = "Elektronika", IsDeleted = false,
                                    LastModifiedDate = DateTime.UtcNow };

        _categoryRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category>());
        _categoryRepoMock.Setup(r => r.AddAsync(It.IsAny<Category>())).ReturnsAsync(entity);

        var result = await _service.AddAsync(dto);

        result.Should().NotBeNull();
        result.Name.Should().Be("Elektronika");
    }

    [Test]
    public async Task AddAsync_DuplicateName_ThrowsMarketException()
    {
        var dto      = new AddCategoryDto { Name = "Elektronika" };
        var existing = new List<Category>
            { new() { Id = 1, Name = "Elektronika", IsDeleted = false, LastModifiedDate = DateTime.UtcNow } };

        _categoryRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(existing);

        Func<Task> act = () => _service.AddAsync(dto);

        await act.Should().ThrowAsync<MarketException>().WithMessage("*allaqachon mavjud*");
    }

    [Test]
    public async Task AddAsync_NullDto_ThrowsArgumentNullException()
    {
        Func<Task> act = () => _service.AddAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Test]
    public async Task AddAsync_EmptyName_ThrowsMarketException()
    {
        var dto = new AddCategoryDto { Name = "" };
        Func<Task> act = () => _service.AddAsync(dto);
        await act.Should().ThrowAsync<MarketException>();
    }

    // ─── GetAll* ─────────────────────────────────────────────────────────

    [Test]
    public async Task GetAllActivesAsync_ReturnsOnlyNonDeleted()
    {
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Active1",  IsDeleted = false, LastModifiedDate = DateTime.UtcNow },
            new() { Id = 2, Name = "Deleted1", IsDeleted = true,  LastModifiedDate = DateTime.UtcNow },
            new() { Id = 3, Name = "Active2",  IsDeleted = false, LastModifiedDate = DateTime.UtcNow }
        };
        _categoryRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(categories);

        var result = await _service.GetAllActivesAsync();

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(c => c.IsDeleted.Should().BeFalse());
    }

    [Test]
    public async Task GetAllArchivesAsync_ReturnsOnlyDeleted()
    {
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Active1",  IsDeleted = false, LastModifiedDate = DateTime.UtcNow },
            new() { Id = 2, Name = "Deleted1", IsDeleted = true,  LastModifiedDate = DateTime.UtcNow }
        };
        _categoryRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(categories);

        var result = await _service.GetAllArchivesAsync();

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Deleted1");
    }

    // ─── ActionAsync ─────────────────────────────────────────────────────

    [Test]
    public async Task ActionAsync_Archive_SetsIsDeletedTrue()
    {
        var category = new Category { Id = 5, Name = "Test", IsDeleted = false, LastModifiedDate = DateTime.UtcNow };
        _categoryRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(category);
        _categoryRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Category>())).ReturnsAsync(category);

        await _service.ActionAsync(5, ActionType.Archive);

        _categoryRepoMock.Verify(r => r.UpdateAsync(It.Is<Category>(c => c.IsDeleted)), Times.Once);
    }

    [Test]
    public async Task ActionAsync_Recover_SetsIsDeletedFalse()
    {
        var category = new Category { Id = 5, Name = "Test", IsDeleted = true, LastModifiedDate = DateTime.UtcNow };
        _categoryRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(category);
        _categoryRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Category>())).ReturnsAsync(category);

        await _service.ActionAsync(5, ActionType.Recover);

        _categoryRepoMock.Verify(r => r.UpdateAsync(It.Is<Category>(c => !c.IsDeleted)), Times.Once);
    }

    // ─── GetByIdAsync ─────────────────────────────────────────────────────

    [Test]
    public async Task GetByIdAsync_NotFound_ThrowsMarketException()
    {
        _categoryRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Category?)null);

        Func<Task> act = () => _service.GetByIdAsync(999);
        await act.Should().ThrowAsync<MarketException>();
    }

    // ─── FilterByNameAsync ───────────────────────────────────────────────

    [Test]
    public async Task FilterByNameAsync_ReturnsMatchingByName()
    {
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Elektronika",         IsDeleted = false, LastModifiedDate = DateTime.UtcNow },
            new() { Id = 2, Name = "Oziq-ovqat",          IsDeleted = false, LastModifiedDate = DateTime.UtcNow },
            new() { Id = 3, Name = "Elektronika qurilmalar", IsDeleted = false, LastModifiedDate = DateTime.UtcNow }
        };
        _categoryRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(categories);

        var result = await _service.FilterByNameAsync("elektron", State.Active);

        result.Should().HaveCount(2);
    }

    // ─── Cache ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetAllAsync_SecondCall_UsesCacheNotDb()
    {
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Cat1", IsDeleted = false, LastModifiedDate = DateTime.UtcNow }
        };
        _categoryRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(categories);

        await _service.GetAllAsync(); // 1-chi chaqiruv — DB
        await _service.GetAllAsync(); // 2-chi chaqiruv — cache

        // DB faqat 1 marta chaqirilishi kerak
        _categoryRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }
}
