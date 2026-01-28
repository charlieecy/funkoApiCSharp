using FluentAssertions;
using FunkoApi.DataBase;
using FunkoApi.Models;
using FunkoApi.Repository.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace FunkoTest.Repository;

[TestFixture]
public class UserRepositoryTests
{
    private UserRepository _repository = null!;
    private Context _context = null!;
    private Mock<ILogger<UserRepository>> _loggerMock = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<Context>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new Context(options);
        _context.Database.EnsureCreated(); 

        _loggerMock = new Mock<ILogger<UserRepository>>();
        _repository = new UserRepository(_context, _loggerMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Test]
    public async Task FindByIdAsync_ExistingId_ReturnsUser()
    {
        // Arrange 
        long id = 1;

        // Act
        var result = await _repository.FindByIdAsync(id);

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("admin");
    }

    [Test]
    public async Task FindByIdAsync_NonExistingId_ReturnsNull()
    {
        // Act
        var result = await _repository.FindByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task FindByUsernameAsync_ExistingUsername_ReturnsUser()
    {
        // Act
        var result = await _repository.FindByUsernameAsync("admin");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("admin@funkoapi.com");
    }

    [Test]
    public async Task FindByUsernameAsync_NonExistingUsername_ReturnsNull()
    {
        // Act
        var result = await _repository.FindByUsernameAsync("ghost");

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task FindByEmailAsync_ExistingEmail_ReturnsUser()
    {
        // Act
        var result = await _repository.FindByEmailAsync("user@funkoapi.com");

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("user");
    }

    [Test]
    public async Task FindByEmailAsync_NonExistingEmail_ReturnsNull()
    {
        // Act
        var result = await _repository.FindByEmailAsync("fake@email.com");

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task FindAllAsync_ReturnsAllUsers()
    {
        // Act
        var result = await _repository.FindAllAsync();

        // Assert
        result.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Test]
    public async Task SaveAsync_NewUser_AddsToDatabase()
    {
        // Arrange
        var newUser = new User
        {
            Username = "newuser",
            Email = "new@test.com",
            PasswordHash = "hash",
            Role = User.UserRoles.USER,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _repository.SaveAsync(newUser);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        
        var inDb = await _context.Users.FindAsync(result.Id);
        inDb.Should().NotBeNull();
        inDb!.Username.Should().Be("newuser");
    }

    [Test]
    public async Task UpdateAsync_ExistingUser_UpdatesFields()
    {
        // Arrange
        long id = 2; // User
        var userToUpdate = await _context.Users.FindAsync(id);
        userToUpdate!.Username = "updatedName";

        // Act
        var result = await _repository.UpdateAsync(userToUpdate);

        // Assert
        result.Username.Should().Be("updatedName");
        
        var inDb = await _context.Users.FindAsync(id);
        inDb!.Username.Should().Be("updatedName");
    }

    [Test]
    public async Task DeleteAsync_ExistingUser_SoftDeletesCorrectly()
    {
        // Arrange
        long id = 2; // User

        // Act
        await _repository.DeleteAsync(id);
        
        var inDb = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id);
        
        inDb.Should().NotBeNull();
        inDb!.IsDeleted.Should().BeTrue();
    }

    [Test]
    public async Task DeleteAsync_NonExistingUser_DoesNothing()
    {
        // Arrange
        long id = 999;

        // Act
        // Should not throw
        await _repository.DeleteAsync(id);

        // Assert
        
    }

    [Test]
    public async Task GetActiveUsersAsync_ReturnsOnlyNonDeleted()
    {
        // Arrange
        var userToDelete = await _context.Users.FindAsync(2L);
        userToDelete!.IsDeleted = true;
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveUsersAsync();

        // Assert
        result.Should().OnlyContain(u => !u.IsDeleted);
        result.Should().Contain(u => u.Username == "admin");
        result.Should().NotContain(u => u.Id == 2);
    }
}
