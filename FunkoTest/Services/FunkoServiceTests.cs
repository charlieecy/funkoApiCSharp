using FluentAssertions;
using Moq;
using NUnit.Framework;
using FunkoApi.Services;
using FunkoApi.Repository;
using FunkoApi.DTO;
using FunkoApi.Models;
using Microsoft.Extensions.Caching.Distributed;
using FunkoApi.GraphQL.Publisher;
using FunkoApi.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.SignalR;
using FunkoApi.SignalR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FunkoTest.Services;

[TestFixture]
public class FunkoServiceTests
{
    private Mock<IDistributedCache> _cacheMock = null!;
    private Mock<IFunkoRepository> _funkoRepositoryMock = null!;
    private Mock<ICategoryRepository> _categoryRepositoryMock = null!;
    private Mock<IEventPublisher> _eventPublisherMock = null!;
    private Mock<IEmailService> _emailServiceMock = null!;
    private Mock<IConfiguration> _configurationMock = null!;
    private Mock<IHubContext<FunkoHub>> _hubContextMock = null!;
    private Mock<ILogger<FunkoService>> _loggerMock = null!;
    private FunkoService _service = null!;
    private Mock<IHubClients> _clientsMock = null!;
    private Mock<IClientProxy> _clientProxyMock = null!;

    [SetUp]
    public void SetUp()
    {
        _cacheMock = new Mock<IDistributedCache>();
        _funkoRepositoryMock = new Mock<IFunkoRepository>();
        _categoryRepositoryMock = new Mock<ICategoryRepository>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _emailServiceMock = new Mock<IEmailService>();
        _configurationMock = new Mock<IConfiguration>();
        _hubContextMock = new Mock<IHubContext<FunkoHub>>();
        _loggerMock = new Mock<ILogger<FunkoService>>();
        
        _clientsMock = new Mock<IHubClients>();
        _clientProxyMock = new Mock<IClientProxy>();
        _clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _hubContextMock.Setup(h => h.Clients).Returns(_clientsMock.Object);

        _service = new FunkoService(
            _cacheMock.Object,
            _funkoRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _eventPublisherMock.Object,
            _emailServiceMock.Object,
            _configurationMock.Object,
            _hubContextMock.Object,
            _loggerMock.Object
        );
    }

    [Test]
    public async Task GetByIdAsync_FunkoExistenteEnCache_ReturnSuccess()
    {
        var funkoId = 1L;
        var categoryId = Guid.NewGuid();
        
        var funkoEsperado = new Funko
        {
            Id = funkoId,
            Nombre = "Iron Man",
            Precio = 29.99,
            Imagen = "ironman.jpg",
            CategoryId = categoryId,
            Category = new Category
            {
                Id = categoryId,
                Nombre = "Marvel",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var serializedFunko = JsonSerializer.Serialize(funkoEsperado);
        var cachedBytes = System.Text.Encoding.UTF8.GetBytes(serializedFunko);
        
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default))
            .ReturnsAsync(cachedBytes);

        var resultado = await _service.GetByIdAsync(funkoId);

        resultado.Should().NotBeNull();
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().NotBeNull();
        resultado.Value.Nombre.Should().Be("Iron Man");
        resultado.Value.Precio.Should().Be(29.99);
        resultado.Value.Categoria.Should().Be("Marvel");
        _funkoRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<long>()), Times.Never);
    }

    [Test]
    public async Task GetByIdAsync_FunkoExistenteEnBD_ReturnSuccess()
    {
        var funkoId = 2L;
        var categoryId = Guid.NewGuid();
        
        var funkoEsperado = new Funko
        {
            Id = funkoId,
            Nombre = "Batman",
            Precio = 34.99,
            Imagen = "batman.jpg",
            CategoryId = categoryId,
            Category = new Category
            {
                Id = categoryId,
                Nombre = "DC Comics",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default))
            .ReturnsAsync((byte[]?)null);
        _funkoRepositoryMock.Setup(r => r.GetByIdAsync(funkoId))
            .ReturnsAsync(funkoEsperado);
        _cacheMock.Setup(c => c.SetAsync(
            It.IsAny<string>(), 
            It.IsAny<byte[]>(), 
            It.IsAny<DistributedCacheEntryOptions>(), 
            default))
            .Returns(Task.CompletedTask);

        var resultado = await _service.GetByIdAsync(funkoId);

        resultado.Should().NotBeNull();
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().NotBeNull();
        resultado.Value.Nombre.Should().Be("Batman");
        resultado.Value.Precio.Should().Be(34.99);
        resultado.Value.Categoria.Should().Be("DC Comics");
        _funkoRepositoryMock.Verify(r => r.GetByIdAsync(funkoId), Times.Once);
        _cacheMock.Verify(c => c.SetAsync(
            It.IsAny<string>(), 
            It.IsAny<byte[]>(), 
            It.IsAny<DistributedCacheEntryOptions>(), 
            default), Times.Once);
    }

    [Test]
    public async Task GetByIdAsync_FunkoNoExiste_ReturnFailure()
    {
        var funkoId = 999L;

        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default))
            .ReturnsAsync((byte[]?)null);
        _funkoRepositoryMock.Setup(r => r.GetByIdAsync(funkoId))
            .ReturnsAsync((Funko?)null);

        var resultado = await _service.GetByIdAsync(funkoId);

        resultado.Should().NotBeNull();
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().NotBeNull();
        resultado.Error.Message.Should().Contain("No se encontró el Funko");
    }

    [Test]
    public async Task CreateAsync_CategoriaExiste_ReturnSuccess()
    {
        var dto = new FunkoPostPutRequestDTO
        {
            Nombre = "Spider-Man",
            Categoria = "Marvel",
            Precio = 39.99,
            Imagen = "spiderman.jpg"
        };

        var categoryId = Guid.NewGuid();
        var category = new Category
        {
            Id = categoryId,
            Nombre = "Marvel",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var funkoCreado = new Funko
        {
            Id = 3,
            Nombre = "Spider-Man",
            Precio = 39.99,
            Imagen = "spiderman.jpg",
            CategoryId = categoryId,
            Category = category,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _categoryRepositoryMock.Setup(r => r.GetByNameAsync("Marvel"))
            .ReturnsAsync(category);
        _funkoRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<Funko>()))
            .ReturnsAsync(funkoCreado);

        var resultado = await _service.CreateAsync(dto);

        resultado.Should().NotBeNull();
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().NotBeNull();
        resultado.Value.Nombre.Should().Be("Spider-Man");
        resultado.Value.Precio.Should().Be(39.99);
        resultado.Value.Categoria.Should().Be("Marvel");
        _categoryRepositoryMock.Verify(r => r.GetByNameAsync("Marvel"), Times.Once);
        _funkoRepositoryMock.Verify(r => r.CreateAsync(It.Is<Funko>(f => 
            f.Nombre == "Spider-Man" && 
            f.CategoryId == categoryId)), Times.Once);
    }

    [Test]
    public async Task CreateAsync_CategoriaNoExiste_ReturnFailure()
    {
        var dto = new FunkoPostPutRequestDTO
        {
            Nombre = "Naruto",
            Categoria = "Anime",
            Precio = 29.99,
            Imagen = "naruto.jpg"
        };

        _categoryRepositoryMock.Setup(r => r.GetByNameAsync("Anime"))
            .ReturnsAsync((Category?)null);

        var resultado = await _service.CreateAsync(dto);

        resultado.Should().NotBeNull();
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().NotBeNull();
        resultado.Error.Message.Should().Contain("La categoría: Anime no existe");
        _funkoRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Funko>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_FunkoYCategoriaExisten_ReturnSuccess()
    {
        var funkoId = 1L;
        var dto = new FunkoPostPutRequestDTO
        {
            Nombre = "Iron Man Mark 50",
            Categoria = "Marvel",
            Precio = 49.99,
            Imagen = "ironman50.jpg"
        };

        var categoryId = Guid.NewGuid();
        var category = new Category
        {
            Id = categoryId,
            Nombre = "Marvel",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var funkoActualizado = new Funko
        {
            Id = funkoId,
            Nombre = "Iron Man Mark 50",
            Precio = 49.99,
            Imagen = "ironman50.jpg",
            CategoryId = categoryId,
            Category = category,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow
        };

        _categoryRepositoryMock.Setup(r => r.GetByNameAsync("Marvel"))
            .ReturnsAsync(category);
        _funkoRepositoryMock.Setup(r => r.UpdateAsync(funkoId, It.IsAny<Funko>()))
            .ReturnsAsync(funkoActualizado);

        var resultado = await _service.UpdateAsync(funkoId, dto);

        resultado.Should().NotBeNull();
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().NotBeNull();
        resultado.Value.Nombre.Should().Be("Iron Man Mark 50");
        resultado.Value.Precio.Should().Be(49.99);
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), default), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_CategoriaNoExiste_ReturnFailure()
    {
        var funkoId = 1L;
        var dto = new FunkoPostPutRequestDTO
        {
            Nombre = "Test",
            Categoria = "CategoriaInexistente",
            Precio = 49.99,
            Imagen = "test.jpg"
        };

        _categoryRepositoryMock.Setup(r => r.GetByNameAsync("CategoriaInexistente"))
            .ReturnsAsync((Category?)null);

        var resultado = await _service.UpdateAsync(funkoId, dto);

        resultado.Should().NotBeNull();
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Message.Should().Contain("La categoría: CategoriaInexistente no existe");
    }

    [Test]
    public async Task UpdateAsync_FunkoNoExiste_ReturnFailure()
    {
        var funkoId = 999L;
        var dto = new FunkoPostPutRequestDTO
        {
            Nombre = "Ghost Rider",
            Categoria = "Marvel",
            Precio = 44.99,
            Imagen = "ghostrider.jpg"
        };

        var categoryId = Guid.NewGuid();
        var category = new Category
        {
            Id = categoryId,
            Nombre = "Marvel",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _categoryRepositoryMock.Setup(r => r.GetByNameAsync("Marvel"))
            .ReturnsAsync(category);
        _funkoRepositoryMock.Setup(r => r.UpdateAsync(funkoId, It.IsAny<Funko>()))
            .ReturnsAsync((Funko?)null);

        var resultado = await _service.UpdateAsync(funkoId, dto);

        resultado.Should().NotBeNull();
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().NotBeNull();
        resultado.Error.Message.Should().Contain("No se encontró el Funko");
    }

    [Test]
    public async Task DeleteAsync_FunkoExiste_ReturnSuccess()
    {
        var funkoId = 5L;
        var categoryId = Guid.NewGuid();
        var category = new Category
        {
            Id = categoryId,
            Nombre = "Marvel",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var funkoEliminado = new Funko
        {
            Id = funkoId,
            Nombre = "Wolverine",
            Precio = 39.99,
            Imagen = "wolverine.jpg",
            CategoryId = categoryId,
            Category = category,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _funkoRepositoryMock.Setup(r => r.DeleteAsync(funkoId))
            .ReturnsAsync(funkoEliminado);

        var resultado = await _service.DeleteAsync(funkoId);

        resultado.Should().NotBeNull();
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().NotBeNull();
        resultado.Value.Nombre.Should().Be("Wolverine");
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), default), Times.Once);
        _funkoRepositoryMock.Verify(r => r.DeleteAsync(funkoId), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_FunkoNoExiste_ReturnFailure()
    {
        var funkoId = 999L;

        _funkoRepositoryMock.Setup(r => r.DeleteAsync(funkoId))
            .ReturnsAsync((Funko?)null);

        var resultado = await _service.DeleteAsync(funkoId);

        resultado.Should().NotBeNull();
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().NotBeNull();
        resultado.Error.Message.Should().Contain("No se encontró el Funko");
    }

    [Test]
    public async Task GetAllAsync_ConFiltros_ReturnSuccess()
    {
        var filter = new FilterDTO("Iron", "Marvel", 50.0, 1, 10, "id", "asc");

        var categoryId = Guid.NewGuid();
        var category = new Category
        {
            Id = categoryId,
            Nombre = "Marvel",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var funkos = new List<Funko>
        {
            new()
            {
                Id = 1,
                Nombre = "Iron Man",
                Precio = 29.99,
                Imagen = "ironman.jpg",
                CategoryId = categoryId,
                Category = category,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 2,
                Nombre = "Iron Patriot",
                Precio = 34.99,
                Imagen = "ironpatriot.jpg",
                CategoryId = categoryId,
                Category = category,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _funkoRepositoryMock.Setup(r => r.GetAllAsync(filter))
            .ReturnsAsync((funkos, 2));

        var resultado = await _service.GetAllAsync(filter);

        resultado.Should().NotBeNull();
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().NotBeNull();
        resultado.Value.Items.Should().HaveCount(2);
        resultado.Value.TotalCount.Should().Be(2);
        resultado.Value.Page.Should().Be(1);
        resultado.Value.Size.Should().Be(10);
        resultado.Value.Items.Should().AllSatisfy(f => f.Nombre.Should().Contain("Iron"));
    }

    [Test]
    public async Task PatchAsync_FunkoNoExiste_ReturnFailure()
    {
        var funkoId = 999L;
        var dto = new FunkoPatchRequestDTO { Nombre = "Test" };

        _funkoRepositoryMock.Setup(r => r.GetByIdAsync(funkoId))
            .ReturnsAsync((Funko?)null);

        var resultado = await _service.PatchAsync(funkoId, dto);

        resultado.Should().NotBeNull();
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Message.Should().Contain("no encontrado");
    }

    [Test]
    public async Task PatchAsync_ActualizarSoloNombre_ReturnSuccess()
    {
        var funkoId = 1L;
        var categoryId = Guid.NewGuid();
        var category = new Category
        {
            Id = categoryId,
            Nombre = "Marvel",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var funkoExistente = new Funko
        {
            Id = funkoId,
            Nombre = "Iron Man",
            Precio = 29.99,
            Imagen = "ironman.jpg",
            CategoryId = categoryId,
            Category = category,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var dto = new FunkoPatchRequestDTO { Nombre = "Iron Man Actualizado" };

        _funkoRepositoryMock.Setup(r => r.GetByIdAsync(funkoId))
            .ReturnsAsync(funkoExistente);
        _funkoRepositoryMock.Setup(r => r.UpdateAsync(funkoId, It.IsAny<Funko>()))
            .ReturnsAsync(funkoExistente);

        var resultado = await _service.PatchAsync(funkoId, dto);

        resultado.Should().NotBeNull();
        resultado.IsSuccess.Should().BeTrue();
        funkoExistente.Nombre.Should().Be("Iron Man Actualizado");
    }

    [Test]
    public async Task PatchAsync_ActualizarSoloPrecio_ReturnSuccess()
    {
        var funkoId = 1L;
        var categoryId = Guid.NewGuid();
        var category = new Category
        {
            Id = categoryId,
            Nombre = "Marvel",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var funkoExistente = new Funko
        {
            Id = funkoId,
            Nombre = "Iron Man",
            Precio = 29.99,
            Imagen = "ironman.jpg",
            CategoryId = categoryId,
            Category = category,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var dto = new FunkoPatchRequestDTO { Precio = 49.99 };

        _funkoRepositoryMock.Setup(r => r.GetByIdAsync(funkoId))
            .ReturnsAsync(funkoExistente);
        _funkoRepositoryMock.Setup(r => r.UpdateAsync(funkoId, It.IsAny<Funko>()))
            .ReturnsAsync(funkoExistente);

        var resultado = await _service.PatchAsync(funkoId, dto);

        resultado.Should().NotBeNull();
        resultado.IsSuccess.Should().BeTrue();
        funkoExistente.Precio.Should().Be(49.99);
    }

    [Test]
    public async Task PatchAsync_ActualizarSoloImagen_ReturnSuccess()
    {
        var funkoId = 1L;
        var categoryId = Guid.NewGuid();
        var category = new Category
        {
            Id = categoryId,
            Nombre = "Marvel",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var funkoExistente = new Funko
        {
            Id = funkoId,
            Nombre = "Iron Man",
            Precio = 29.99,
            Imagen = "ironman.jpg",
            CategoryId = categoryId,
            Category = category,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var dto = new FunkoPatchRequestDTO { Imagen = "nueva-imagen.jpg" };

        _funkoRepositoryMock.Setup(r => r.GetByIdAsync(funkoId))
            .ReturnsAsync(funkoExistente);
        _funkoRepositoryMock.Setup(r => r.UpdateAsync(funkoId, It.IsAny<Funko>()))
            .ReturnsAsync(funkoExistente);

        var resultado = await _service.PatchAsync(funkoId, dto);

        resultado.Should().NotBeNull();
        resultado.IsSuccess.Should().BeTrue();
        funkoExistente.Imagen.Should().Be("nueva-imagen.jpg");
    }

    [Test]
    public async Task PatchAsync_ActualizarCategoria_CategoriaExiste_ReturnSuccess()
    {
        var funkoId = 1L;
        var categoryIdOriginal = Guid.NewGuid();
        var categoryOriginal = new Category
        {
            Id = categoryIdOriginal,
            Nombre = "Marvel",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var categoryIdNueva = Guid.NewGuid();
        var categoriaNueva = new Category
        {
            Id = categoryIdNueva,
            Nombre = "DC Comics",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var funkoExistente = new Funko
        {
            Id = funkoId,
            Nombre = "Iron Man",
            Precio = 29.99,
            Imagen = "ironman.jpg",
            CategoryId = categoryIdOriginal,
            Category = categoryOriginal,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var dto = new FunkoPatchRequestDTO { Categoria = "DC Comics" };

        _funkoRepositoryMock.Setup(r => r.GetByIdAsync(funkoId))
            .ReturnsAsync(funkoExistente);
        _categoryRepositoryMock.Setup(r => r.GetByNameAsync("DC Comics"))
            .ReturnsAsync(categoriaNueva);
        _funkoRepositoryMock.Setup(r => r.UpdateAsync(funkoId, It.IsAny<Funko>()))
            .ReturnsAsync(funkoExistente);

        var resultado = await _service.PatchAsync(funkoId, dto);

        resultado.Should().NotBeNull();
        resultado.IsSuccess.Should().BeTrue();
        funkoExistente.CategoryId.Should().Be(categoryIdNueva);
        funkoExistente.Category.Should().Be(categoriaNueva);
    }

    [Test]
    public async Task PatchAsync_ActualizarCategoria_CategoriaNoExiste_ReturnFailure()
    {
        var funkoId = 1L;
        var categoryId = Guid.NewGuid();
        var category = new Category
        {
            Id = categoryId,
            Nombre = "Marvel",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var funkoExistente = new Funko
        {
            Id = funkoId,
            Nombre = "Iron Man",
            Precio = 29.99,
            Imagen = "ironman.jpg",
            CategoryId = categoryId,
            Category = category,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var dto = new FunkoPatchRequestDTO { Categoria = "Anime" };

        _funkoRepositoryMock.Setup(r => r.GetByIdAsync(funkoId))
            .ReturnsAsync(funkoExistente);
        _categoryRepositoryMock.Setup(r => r.GetByNameAsync("Anime"))
            .ReturnsAsync((Category?)null);

        var resultado = await _service.PatchAsync(funkoId, dto);

        resultado.Should().NotBeNull();
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Message.Should().Contain("La categoría: Anime no existe");
    }

    [Test]
    public async Task PatchAsync_ActualizarMultiplesCampos_ReturnSuccess()
    {
        var funkoId = 1L;
        var categoryId = Guid.NewGuid();
        var category = new Category
        {
            Id = categoryId,
            Nombre = "Marvel",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var funkoExistente = new Funko
        {
            Id = funkoId,
            Nombre = "Iron Man",
            Precio = 29.99,
            Imagen = "ironman.jpg",
            CategoryId = categoryId,
            Category = category,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var dto = new FunkoPatchRequestDTO 
        { 
            Nombre = "Iron Man Mark 50",
            Precio = 59.99,
            Imagen = "ironman-mark50.jpg"
        };

        _funkoRepositoryMock.Setup(r => r.GetByIdAsync(funkoId))
            .ReturnsAsync(funkoExistente);
        _funkoRepositoryMock.Setup(r => r.UpdateAsync(funkoId, It.IsAny<Funko>()))
            .ReturnsAsync(funkoExistente);

        var resultado = await _service.PatchAsync(funkoId, dto);

        resultado.Should().NotBeNull();
        resultado.IsSuccess.Should().BeTrue();
        funkoExistente.Nombre.Should().Be("Iron Man Mark 50");
        funkoExistente.Precio.Should().Be(59.99);
        funkoExistente.Imagen.Should().Be("ironman-mark50.jpg");
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), default), Times.Once);
    }
}
