using DeliveryFlow.Api.Data;
using DeliveryFlow.Api.Models;
using DeliveryFlow.Api.Services.Assignments;
using DeliveryFlow.Api.Services.Couriers;
using DeliveryFlow.Api.Services.Deliveries;
using DeliveryFlow.Api.Services.Orders;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace DeliveryFlow.Tests.Integration;

public class DeliveryIntegrationTests : IAsyncLifetime
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
    public async Task
        CompleteDelivery_OrderInDelivery_OrderDeliveredAndCourierAvailable()
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
            var deliveryService =
                new DeliveryService(orderService, courierService, db);

            var order = await orderService
                .CreateAsync("Тестовый заказ", 2500);
            var courier = await courierService
                .CreateAsync("Тестовый курьер");

            orderId = order.Id;
            courierId = courier.Id;

            var assignmentResult =
                await assignmentService.AssignCourierAsync(orderId, courierId);
            Assert.Equal(AssignmentOutcome.Success, assignmentResult.Outcome);

            var startResult = await orderService.StartDeliveryAsync(orderId);
            Assert.Equal(StartDeliveryOutcome.Success, startResult.Outcome);

            var result = await deliveryService.CompleteDeliveryAsync(orderId);
            Assert.Equal(CompleteDeliveryOutcome.Success, result.Outcome);
            Assert.Equal(courierId, order.CourierId);
        }

        await using (var db = new DeliveryFlowDbContext(_options))
        {
            var order = await db.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId);
            var courier = await db.Couriers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == courierId);

            Assert.NotNull(order);
            Assert.NotNull(courier);
            Assert.Equal(OrderStatus.Delivered, order.Status);
            Assert.Equal(CourierStatus.Available, courier.Status);
        }
    }
}