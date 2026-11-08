using DeliveryFlow.Api.Data;
using DeliveryFlow.Api.Models;
using DeliveryFlow.Api.Services.Assignments;
using DeliveryFlow.Api.Services.Couriers;
using DeliveryFlow.Api.Services.Orders;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace DeliveryFlow.Tests.Integration;

public class OrderIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:18")
            .Build();

    private DbContextOptions<DeliveryFlowDbContext> _options = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _options = new DbContextOptionsBuilder<DeliveryFlowDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using var db = new DeliveryFlowDbContext(_options);
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() =>
        _postgres.DisposeAsync().AsTask();


    [Fact]
    public async Task CreateOrder_SavesOrderToDatabase()
    {
        // Arrange + Act
        Guid orderId;

        await using (var db = new DeliveryFlowDbContext(_options))
        {
            var service = new OrderService(db);
            var order = await service.CreateAsync(
                "Тестовый заказ", 500m);

            orderId = order.Id;
        }

        // Assert
        await using (var db = new DeliveryFlowDbContext(_options))
        {
            var savedOrder = await db.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId);

            Assert.NotNull(savedOrder);
            Assert.Equal("Тестовый заказ", savedOrder.Description);
            Assert.Equal(500m, savedOrder.Price);
            Assert.Equal(OrderStatus.Created, savedOrder.Status);
        }
    }

    [Fact]
    public async Task StartDelivery_CourierAssignedToOrder_OrderInDelivery()
    {
        // Arrange + Act
        Guid orderId;
        Guid courierId;

        await using (var db = new DeliveryFlowDbContext(_options))
        {
            var orderService = new OrderService(db);
            var courierService = new CourierService(db);
            var assignmentService =
                new AssignmentService(orderService, courierService, db);

            var order = await orderService
                .CreateAsync("Тестовый заказ", 3500);
            var courier = await courierService
                .CreateAsync("Тестовый курьер");

            orderId = order.Id;
            courierId = courier.Id;

            var assignmentResult =
                await assignmentService.AssignCourierAsync(orderId, courierId);
            Assert.Equal(AssignmentOutcome.Success, assignmentResult.Outcome);

            var (outcome, _) = await orderService.StartDeliveryAsync(orderId);
            Assert.Equal(StartDeliveryOutcome.Success, outcome);
        }

        // Assert
        await using (var db = new DeliveryFlowDbContext(_options))
        {
            var order = await db.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId);

            Assert.NotNull(order);
            Assert.Equal(OrderStatus.InDelivery, order.Status);
            Assert.Equal(courierId, order.CourierId);
        }
    }
}