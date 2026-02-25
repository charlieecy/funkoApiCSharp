using FluentAssertions;
using FunkoApi.DataBase;
using FunkoApi.DTO;
using FunkoApi.Models;
using FunkoApi.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace FunkoTest.Repository;

[TestFixture]
public class FunkoRepositoryTests
{
    private FunkoRepository _repository = null!;
    private Context _context = null!;
    private Mock<ILogger<FunkoRepository>> _loggerMock = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<Context>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new Context(options);
      
        _context.Database.EnsureCreated();

        _loggerMock = new Mock<ILogger<FunkoRepository>>();
        _repository = new FunkoRepository(_context, _loggerMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Test]
    public async Task GetByIdAsync_ExistingId_ReturnsFunkoWithCategory()
    {
        // Arrange
        long existingId = 1;

        // Act
        var result = await _repository.GetByIdAsync(existingId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(existingId);
        result.Nombre.Should().Be("Pikachu");
        result.Category.Should().NotBeNull();
        result.Category!.Nombre.Should().Be("POKEMON");
    }

    [Test]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        long nonExistingId = 999;

        // Act
        var result = await _repository.GetByIdAsync(nonExistingId);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetAllAsync_NoFilters_ReturnsAllPaged()
    {
        // Arrange
        // (Nombre, Categoria, MaxPrecio, Page, Size, SortBy, Direction)
        var filter = new FilterDTO(null, null, null, 0, 10, "Id", "asc");

        // Act
        var (items, totalCount) = await _repository.GetAllAsync(filter);

        // Assert
        totalCount.Should().Be(10);
        items.Should().HaveCount(10);
    }

    [Test]
    public async Task GetAllAsync_FilterByName_ReturnsMatching()
    {
        // Arrange
        var filter = new FilterDTO("Pikachu", null, null, 0, 10, "Id", "asc");

        // Act
        var (items, totalCount) = await _repository.GetAllAsync(filter);

        // Assert
        totalCount.Should().Be(1);
        items.Single().Nombre.Should().Be("Pikachu");
    }

    [Test]
    public async Task GetAllAsync_FilterByCategory_ReturnsMatching()
    {
        // Arrange
        var filter = new FilterDTO(null, "MARVEL", null, 0, 10, "Id", "asc");

        // Act
        var (items, totalCount) = await _repository.GetAllAsync(filter);

        // Assert
        totalCount.Should().Be(2); 
        items.All(f => f.Category!.Nombre == "MARVEL").Should().BeTrue();
    }

    [Test]
    public async Task GetAllAsync_FilterByMaxPrice_ReturnsBelowOrEqual()
    {
        // Arrange
        var filter = new FilterDTO(null, null, 13.0, 0, 10, "Id", "asc");

        // Act
        var (items, totalCount) = await _repository.GetAllAsync(filter);

        // Assert
        totalCount.Should().Be(2);
        items.All(f => f.Precio <= 13.0).Should().BeTrue();
    }

    [Test]
    public async Task GetAllAsync_Sorting_ReturnsOrdered()
    {
        // Arrange
        var filter = new FilterDTO(null, null, null, 0, 10, "Precio", "desc");

        // Act
        var (items, _) = await _repository.GetAllAsync(filter);

        // Assert
        items.First().Nombre.Should().Be("Arthas (The Lich King)");
    }

    [Test]
    public async Task GetAllAsync_SortingByCreatedAt_ReturnsOrdered()
    {
        // Arrange
        var filter = new FilterDTO(null, null, null, 0, 10, "CreatedAt", "asc");

        // Act
        var (items, _) = await _repository.GetAllAsync(filter);

        // Assert
        items.Should().BeInAscendingOrder(f => f.CreatedAt);
    }

    [Test]
    public async Task GetAllAsync_SortingByCategory_ReturnsOrdered()
    {
        // Arrange
        var filter = new FilterDTO(null, null, null, 0, 10, "Categoria", "asc");

        // Act
        var (items, _) = await _repository.GetAllAsync(filter);

        // Assert
        items.Should().BeInAscendingOrder(f => f.Category!.Nombre);
    }

    [Test]
    public async Task GetAllAsync_SortingByUnknownField_ReturnsOrderedById()
    {
        // Arrange
        var filter = new FilterDTO(null, null, null, 0, 10, "UnknownField", "asc");

        // Act
        var (items, _) = await _repository.GetAllAsync(filter);

        // Assert
        items.First().Id.Should().Be(1);
    }

    [Test]
    public async Task CreateAsync_ValidFunko_AddsToDatabase()
    {
        // Arrange
        var newFunko = new Funko
        {
            Nombre = "New Funko",
            Precio = 10.0,
            CategoryId = Guid.Parse("722f9661-8631-419b-8903-34e9e0339d01"), // Pokemon ID
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _repository.CreateAsync(newFunko);

        // Assert
        result.Id.Should().BeGreaterThan(0); // Generated ID
        
        var inDb = await _context.Funkos.FindAsync(result.Id);
        inDb.Should().NotBeNull();
        inDb!.Nombre.Should().Be("New Funko");
    }

    [Test]
    public async Task UpdateAsync_ExistingFunko_UpdatesAndReturnsFunko()
    {
        // Arrange
        long idToUpdate = 1; // Pikachu
        var updateInfo = new Funko
        {
            Nombre = "Pikachu Updated",
            Precio = 99.99,
            CategoryId = Guid.Parse("722f9661-8631-419b-8903-34e9e0339d01"),
            Imagen = "new_img.png"
        };

        // Act
        var result = await _repository.UpdateAsync(idToUpdate, updateInfo);

        // Assert
        result.Should().NotBeNull();
        result!.Nombre.Should().Be("Pikachu Updated");

        var inDb = await _context.Funkos.FindAsync(idToUpdate);
        inDb!.Nombre.Should().Be("Pikachu Updated");
        inDb.Precio.Should().Be(99.99);
    }

    [Test]
    public async Task UpdateAsync_NonExistingFunko_ReturnsNull()
    {
        // Arrange
        long id = 999;
        var updateInfo = new Funko { Nombre = "Fail" };

        // Act
        var result = await _repository.UpdateAsync(id, updateInfo);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task DeleteAsync_ExistingFunko_RemovesAndReturnsFunko()
    {
        // Arrange
        long idToDelete = 2; // Charmander

        // Act
        var result = await _repository.DeleteAsync(idToDelete);

        // Assert
        result.Should().NotBeNull();
        result!.Nombre.Should().Be("Charmander");

        var inDb = await _context.Funkos.FindAsync(idToDelete);
        inDb.Should().BeNull();
    }

    [Test]
    public async Task DeleteAsync_NonExistingFunko_ReturnsNull()
    {
        // Arrange
        long id = 999;

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
        result.Should().BeAssignableTo<IQueryable<Funko>>();
        result.Count().Should().BeGreaterThanOrEqualTo(10);
    }
}
