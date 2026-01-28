using FluentAssertions;
using FunkoApi.DataBase;
using FunkoApi.Models;
using FunkoApi.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace FunkoTest.Repository;

[TestFixture]
public class CategoryRepositoryTests
{
    private CategoryRepository _repository = null!;
    private Context _context = null!;
    private Mock<ILogger<CategoryRepository>> _loggerMock = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<Context>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new Context(options);
        _context.Database.EnsureCreated(); 

        _loggerMock = new Mock<ILogger<CategoryRepository>>();
        _repository = new CategoryRepository(_context, _loggerMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Test]
    public async Task GetAllAsync_ReturnsAllSeededCategories()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThanOrEqualTo(4); 
        result.Should().Contain(c => c.Nombre == "POKEMON");
        result.Should().Contain(c => c.Nombre == "MARVEL");
    }

    [Test]
    public async Task GetByIdAsync_ExistingId_ReturnsCategory()
    {
        // Arrange
        // Use one of the seeded IDs from Context.cs
        var marvelId = Guid.Parse("2974914c-1123-455b-8d00-4b693e5e463a");

        // Act
        var result = await _repository.GetByIdAsync(marvelId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(marvelId);
        result.Nombre.Should().Be("MARVEL");
    }

    [Test]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistingId);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetByNameAsync_ExistingName_ReturnsCategory()
    {
        // Arrange
        var name = "POKEMON";

        // Act
        var result = await _repository.GetByNameAsync(name);

        // Assert
        result.Should().NotBeNull();
        result!.Nombre.Should().Be("POKEMON");
    }

    [Test]
    public async Task GetByNameAsync_ExistingNameIgnoreCase_ReturnsCategory()
    {
        // Arrange
        var name = "pokemon"; // lowercase

        // Act
        var result = await _repository.GetByNameAsync(name);

        // Assert
        result.Should().NotBeNull();
        result!.Nombre.Should().Be("POKEMON");
    }

    [Test]
    public async Task GetByNameAsync_NonExistingName_ReturnsNull()
    {
        // Arrange
        var name = "Digimon";

        // Act
        var result = await _repository.GetByNameAsync(name);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task CreateAsync_ValidCategory_AddsToDatabase()
    {
        // Arrange
        var newCategory = new Category
        {
            Id = Guid.NewGuid(),
            Nombre = "Anime",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _repository.CreateAsync(newCategory);

        // Assert
        result.Should().NotBeNull();
        result.Nombre.Should().Be("Anime");

        // Verify in DB
        var inDb = await _context.Categories.FindAsync(newCategory.Id);
        inDb.Should().NotBeNull();
        inDb!.Nombre.Should().Be("Anime");
    }

    [Test]
    public async Task UpdateAsync_ExistingCategory_UpdatesAndReturnsCategory()
    {
        // Arrange
        var id = Guid.Parse("3f4e3c98-1e96-487b-9494-28e44e233633"); // WOW
        var updateInfo = new Category
        {
            Nombre = "World of Warcraft",
        };

        // Act
        var result = await _repository.UpdateAsync(id, updateInfo);

        // Assert
        result.Should().NotBeNull();
        result!.Nombre.Should().Be("World of Warcraft");
        
        // Check DB
        var inDb = await _context.Categories.FindAsync(id);
        inDb!.Nombre.Should().Be("World of Warcraft");
        inDb.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Test]
    public async Task UpdateAsync_NonExistingCategory_ReturnsNull()
    {
        // Arrange
        var id = Guid.NewGuid();
        var updateInfo = new Category { Nombre = "New Name" };

        // Act
        var result = await _repository.UpdateAsync(id, updateInfo);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task DeleteAsync_ExistingCategory_RemovesAndReturnsCategory()
    {
        // Arrange
        var id = Guid.Parse("a5b6d5f7-6c2e-4b9e-9e4a-4d2d6f5c8e1a"); // TERROR

        // Act
        var result = await _repository.DeleteAsync(id);

        // Assert
        result.Should().NotBeNull();
        result!.Nombre.Should().Be("TERROR");

        // Verify removed
        var inDb = await _context.Categories.FindAsync(id);
        inDb.Should().BeNull();
    }

    [Test]
    public async Task DeleteAsync_NonExistingCategory_ReturnsNull()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var result = await _repository.DeleteAsync(id);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void FindAllAsNoTracking_ReturnsQueryable()
    {
        // Act
        var result = _repository.FindAllAsNoTracking();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IQueryable<Category>>();
        result.Count().Should().BeGreaterThanOrEqualTo(4);
    }
}
