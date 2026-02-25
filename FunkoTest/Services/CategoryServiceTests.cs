using System.Text;
using System.Text.Json;
using FluentAssertions;
using FunkoApi.DTO;
using FunkoApi.Error;
using FunkoApi.Models;
using FunkoApi.Repository;
using FunkoApi.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;

namespace FunkoTest.Services;

[TestFixture]
public class CategoryServiceTests
{
    private Mock<ICategoryRepository> _repositoryMock = null!;
    private Mock<IDistributedCache> _cacheMock = null!;
    private Mock<ILogger<CategoryService>> _loggerMock = null!;
    private CategoryService _service = null!;

    [SetUp]
    public void Setup()
    {
        _repositoryMock = new Mock<ICategoryRepository>();
        _cacheMock = new Mock<IDistributedCache>();
        _loggerMock = new Mock<ILogger<CategoryService>>();
        _service = new CategoryService(_repositoryMock.Object, _cacheMock.Object, _loggerMock.Object);
    }

    [Test]
    public async Task GetByIdAsync_CategoryExistsInCache_ReturnsSuccess()
    {
        var categoryId = Guid.NewGuid();
        
        var expectedCategory = new Category
        {
            Id = categoryId,
            Nombre = "Marvel",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var serializedCategory = JsonSerializer.Serialize(expectedCategory);
        var cachedBytes = Encoding.UTF8.GetBytes(serializedCategory);
        
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default))
            .ReturnsAsync(cachedBytes);

        var result = await _service.GetByIdAsync(categoryId);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(categoryId.ToString());
        result.Value.Nombre.Should().Be("Marvel");
        _repositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task GetByIdAsync_CategoryNotInCache_FetchesFromDbAndCaches()
    {
        var categoryId = Guid.NewGuid();
        
        var categoryFromDb = new Category
        {
            Id = categoryId,
            Nombre = "DC",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default))
            .ReturnsAsync((byte[])null!);
        
        _repositoryMock.Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(categoryFromDb);

        var result = await _service.GetByIdAsync(categoryId);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Nombre.Should().Be("DC");
        _cacheMock.Verify(c => c.SetAsync(
            It.IsAny<string>(), 
            It.IsAny<byte[]>(), 
            It.IsAny<DistributedCacheEntryOptions>(), 
            default), Times.Once);
    }

    [Test]
    public async Task GetByIdAsync_CategoryNotExists_ReturnsFailure()
    {
        var categoryId = Guid.NewGuid();

        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default))
            .ReturnsAsync((byte[])null!);
        
        _repositoryMock.Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync((Category)null!);

        var result = await _service.GetByIdAsync(categoryId);

        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<FunkoNotFoundError>();
    }

    [Test]
    public async Task GetAllAsync_ReturnsAllCategories()
    {
        var categories = new List<Category>
        {
            new Category { Id = Guid.NewGuid(), Nombre = "Marvel", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Category { Id = Guid.NewGuid(), Nombre = "DC", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Category { Id = Guid.NewGuid(), Nombre = "Star Wars", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        _repositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(categories);

        var result = await _service.GetAllAsync();

        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result[0].Nombre.Should().Be("Marvel");
        result[1].Nombre.Should().Be("DC");
        result[2].Nombre.Should().Be("Star Wars");
    }

    [Test]
    public async Task CreateAsync_CategoryDoesNotExist_ReturnsSuccess()
    {
        var dto = new CategoryPostPutRequestDTO { Nombre = "Anime" };
        var savedCategory = new Category
        {
            Id = Guid.NewGuid(),
            Nombre = "Anime",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.GetByNameAsync("Anime"))
            .ReturnsAsync((Category)null!);
        
        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<Category>()))
            .ReturnsAsync(savedCategory);

        var result = await _service.CreateAsync(dto);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Nombre.Should().Be("Anime");
    }

    [Test]
    public async Task CreateAsync_CategoryAlreadyExists_ReturnsFailure()
    {
        var dto = new CategoryPostPutRequestDTO { Nombre = "Marvel" };
        var existingCategory = new Category
        {
            Id = Guid.NewGuid(),
            Nombre = "Marvel",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.GetByNameAsync("Marvel"))
            .ReturnsAsync(existingCategory);

        var result = await _service.CreateAsync(dto);

        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<FunkoConflictError>();
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Category>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_ValidUpdateWithNoConflict_ReturnsSuccess()
    {
        var categoryId = Guid.NewGuid();
        var dto = new CategoryPostPutRequestDTO { Nombre = "Marvel Updated" };
        var updatedCategory = new Category
        {
            Id = categoryId,
            Nombre = "Marvel Updated",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.GetByNameAsync("Marvel Updated"))
            .ReturnsAsync((Category)null!);
        
        _repositoryMock.Setup(r => r.UpdateAsync(categoryId, It.IsAny<Category>()))
            .ReturnsAsync(updatedCategory);

        var result = await _service.UpdateAsync(categoryId, dto);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Nombre.Should().Be("Marvel Updated");
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), default), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_NameAlreadyExistsInDifferentCategory_ReturnsFailure()
    {
        var categoryId = Guid.NewGuid();
        var dto = new CategoryPostPutRequestDTO { Nombre = "DC" };
        var existingCategory = new Category
        {
            Id = Guid.NewGuid(),
            Nombre = "DC",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.GetByNameAsync("DC"))
            .ReturnsAsync(existingCategory);

        var result = await _service.UpdateAsync(categoryId, dto);

        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<FunkoConflictError>();
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Category>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_CategoryNotFound_ReturnsFailure()
    {
        var categoryId = Guid.NewGuid();
        var dto = new CategoryPostPutRequestDTO { Nombre = "New Name" };

        _repositoryMock.Setup(r => r.GetByNameAsync("New Name"))
            .ReturnsAsync((Category)null!);
        
        _repositoryMock.Setup(r => r.UpdateAsync(categoryId, It.IsAny<Category>()))
            .ReturnsAsync((Category)null!);

        var result = await _service.UpdateAsync(categoryId, dto);

        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<FunkoNotFoundError>();
    }

    [Test]
    public async Task UpdateAsync_SameCategoryUpdatingItsOwnName_ReturnsSuccess()
    {
        var categoryId = Guid.NewGuid();
        var dto = new CategoryPostPutRequestDTO { Nombre = "Marvel v2" };
        var existingWithSameName = new Category
        {
            Id = categoryId,
            Nombre = "Marvel v2",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };
        var updatedCategory = new Category
        {
            Id = categoryId,
            Nombre = "Marvel v2",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.GetByNameAsync("Marvel v2"))
            .ReturnsAsync(existingWithSameName);
        
        _repositoryMock.Setup(r => r.UpdateAsync(categoryId, It.IsAny<Category>()))
            .ReturnsAsync(updatedCategory);

        var result = await _service.UpdateAsync(categoryId, dto);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Nombre.Should().Be("Marvel v2");
    }

    [Test]
    public async Task DeleteAsync_CategoryExists_ReturnsSuccess()
    {
        var categoryId = Guid.NewGuid();
        var deletedCategory = new Category
        {
            Id = categoryId,
            Nombre = "ToDelete",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.DeleteAsync(categoryId))
            .ReturnsAsync(deletedCategory);

        var result = await _service.DeleteAsync(categoryId);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Nombre.Should().Be("ToDelete");
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), default), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_CategoryNotFound_ReturnsFailure()
    {
        var categoryId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.DeleteAsync(categoryId))
            .ReturnsAsync((Category)null!);

        var result = await _service.DeleteAsync(categoryId);

        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<FunkoNotFoundError>();
    }
}
