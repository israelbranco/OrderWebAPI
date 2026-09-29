using Moq;
using OrderWebAPI.Application.DTOs;
using OrderWebAPI.Application.Exceptions;
using OrderWebAPI.Application.Interfaces;
using OrderWebAPI.Application.Services; // Ajuste para o namespace correto do seu serviço
using OrderWebAPI.Domain.Entities;
using OrderWebAPI.Domain.Enums;

public class OrderServiceIntegrationTests
{
    private readonly Mock<IOrderRepository> _orderRepoMock;
    private readonly Mock<IProductRepository> _productRepoMock;
    private readonly OrderService _orderService; // Substitua pelo nome real da sua classe de serviço (ex: OrderApplicationService)

    public OrderServiceIntegrationTests()
    {
        _orderRepoMock = new Mock<IOrderRepository>();
        _productRepoMock = new Mock<IProductRepository>();

        // Instancia o serviço injetando os mocks dos repositórios
        _orderService = new OrderService(_orderRepoMock.Object, _productRepoMock.Object);
    }

    [Fact]
    public async Task CreateOrderAsync_DeveCriarPedido_QuandoDtoValidoForFornecido()
    {
        // Arrange
        // 1. Cria o produto (a entidade gera o ID internamente)
        var product = new Product("Produto Teste", 100.0m, 10);

        var customerId = Guid.NewGuid();

        // 2. Usa o product.Id real para o DTO de criação do item
        var items = new List<CreateOrderItemDto>
    {
        new CreateOrderItemDto(product.Id, 2)
    };

        var dto = new CreateOrderDto(customerId, "BRL", items);

        _productRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<Product> { product });

        _orderRepoMock.Setup(r => r.AddAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _orderService.CreateOrderAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.CustomerId, result.CustomerId);
        _orderRepoMock.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_DeveLancarExcecaoDeValidacao_QuandoOEstoqueForInsuficiente()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product("Produto Teste", 100.0m, 1); // Estoque apenas 1

        var customerId = Guid.NewGuid();
        var items = new List<CreateOrderItemDto>
        {
            new CreateOrderItemDto(productId, 5) // Pedindo 5 (maior que o estoque)
        };

        // CORREÇÃO AQUI: Passando os parâmetros direto no construtor do record
        var dto = new CreateOrderDto(customerId, "BRL", items);

        _productRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<Product> { product });

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _orderService.CreateOrderAsync(dto));
    }

    [Fact]
    public async Task ConfirmOrderAsync_DeveConfirmarPedidoEReduzirEstoque_QuandoStatusForRealizado()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        // 1. Cria o produto (a entidade gera o ID internamente)
        var product = new Product("Produto Teste", 50.0m, 10);

        // 2. Usa o product.Id real para criar o item do pedido
        var orderItem = new OrderItem(product.Id, 50.0m, 3);
        var order = new Order(Guid.NewGuid(), "BRL", new List<OrderItem> { orderItem });
        // Por padrão, o novo pedido nasce com status 'Placed'

        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        // 3. Configura o mock para retornar o produto correto na busca
        _productRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<Product> { product });

        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);

        // Act
        await _orderService.ConfirmOrderAsync(orderId);

        // Assert
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        _orderRepoMock.Verify(r => r.UpdateAsync(order), Times.Once);
    }

    [Fact]
    public async Task ConfirmOrderAsync_DeveSerIdempotente_QuandoJaEstiverConfirmado()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        // Cria um item válido para satisfazer a regra de negócio do construtor de Order
        var orderItem = new OrderItem(productId, 50.0m, 1);

        // Passa o item na lista
        var order = new Order(Guid.NewGuid(), "BRL", new List<OrderItem> { orderItem });
        order.Confirm(); // Já confirmado

        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        // Act & Assert (Não deve lançar exceção e não deve chamar update)
        await _orderService.ConfirmOrderAsync(orderId);

        _orderRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CancelOrderAsync_DeveRetornarEstoqueECancelar_QuandoOPedidoTiverSidoConfirmado()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        // 1. Cria o produto (a entidade gera o ID dela internamente)
        var product = new Product("Produto Teste", 50.0m, 5);

        // 2. Usa o ID real gerado pela entidade para criar o item do pedido
        var orderItem = new OrderItem(product.Id, 50.0m, 2);
        var order = new Order(Guid.NewGuid(), "BRL", new List<OrderItem> { orderItem });
        order.Confirm(); // Deixa o pedido como confirmado

        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        // 3. Configura o mock para retornar a lista contendo o produto criado
        _productRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new List<Product> { product });

        _orderRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);

        // Act
        await _orderService.CancelOrderAsync(orderId);

        // Assert
        Assert.Equal(OrderStatus.Canceled, order.Status);
        Assert.Equal(7, product.AvailableQuantity); // 5 originais + 2 devolvidos
        _orderRepoMock.Verify(r => r.UpdateAsync(order), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_DeveRetornarDto_QuandoPedidoExistir()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        // 1. Cria um item de pedido válido para satisfazer a regra do construtor
        var orderItem = new OrderItem(productId, 50.0m, 2);

        // 2. Cria o pedido passando o item na lista
        var order = new Order(Guid.NewGuid(), "BRL", new List<OrderItem> { orderItem });

        // Configura o mock para retornar o pedido quando o serviço buscar pelo ID
        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        // Act
        var result = await _orderService.GetByIdAsync(orderId);

        // Assert
        Assert.NotNull(result);
        // Valide os campos do DTO retornado, por exemplo:
        // Assert.Equal(orderId, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_DeveRetornarNulo_QuandoOPedidoNaoExistir()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync((Order?)null);

        // Act
        var result = await _orderService.GetByIdAsync(orderId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPagedAsync_DeveRetornarPedidosPaginadosETotalDeRegistros()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // 1. Cria um item de pedido válido para satisfazer a regra do construtor
        var orderItem = new OrderItem(productId, 50.0m, 1);

        // 2. Instancia os pedidos passando a lista com o item obrigatório
        var orders = new List<Order>
    {
        new Order(Guid.NewGuid(), "BRL", new List<OrderItem> { orderItem }),
        new Order(Guid.NewGuid(), "BRL", new List<OrderItem> { orderItem })
    };

        // Configura o mock do repositório para aceitar os parâmetros de busca
        _orderRepoMock.Setup(r => r.GetPagedAsync(
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync((orders, 2));

        //Passando null para os filtros opcionais e (1, 10) para página e tamanho
        var result = await _orderService.GetPagedAsync(null, null, null, null, 1, 10);

        // Assert
        Assert.NotNull(result);
        // Valide os itens da paginação conforme a estrutura do seu DTO de retorno (ex: result.Items, result.TotalCount, etc.)
    }
}