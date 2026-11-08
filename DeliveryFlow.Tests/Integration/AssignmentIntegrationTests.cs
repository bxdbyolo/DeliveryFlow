using DeliveryFlow.Api.Data;
using DeliveryFlow.Api.Models;
using DeliveryFlow.Api.Services.Assignments;
using DeliveryFlow.Api.Services.Couriers;
using DeliveryFlow.Api.Services.Orders;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace DeliveryFlow.Tests.Integration;

public class AssignmentIntegrationTests : IAsyncLifetime
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
        AssignCourier_CreatedOrderAndAvailableCourier_PersistsChanges()
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
                .CreateAsync("Тестовый заказ", 2500);
            var courier = await courierService
                .CreateAsync("Тестовый курьер");

            orderId = order.Id;
            courierId = courier.Id;


            var result = await assignmentService
                .AssignCourierAsync(orderId, courierId);

            Assert.Equal(AssignmentOutcome.Success, result.Outcome);
        }

        // Assert
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
            Assert.Equal(OrderStatus.Assigned, order.Status);
            Assert.Equal(courierId, order.CourierId);
            Assert.Equal(CourierStatus.Busy, courier.Status);
        }
    }

    [Fact]
    public async Task AssignCourier_BusyCourier_DoesNotChangeSecondOrder()
    {
        // Arrange + Act
        Guid firstOrderId, secondOrderId;
        Guid courierId;

        await using (var db = new DeliveryFlowDbContext(_options))
        {
            var orderService = new OrderService(db);
            var courierService = new CourierService(db);
            var assignmentService =
                new AssignmentService(orderService, courierService, db);

            var firstOrder = await orderService
                .CreateAsync("Тестовый заказ 1", 2500);
            var secondOrder = await orderService
                .CreateAsync("Тестовый заказ 2", 3500);
            var courier = await courierService
                .CreateAsync("Тестовый курьер");

            firstOrderId = firstOrder.Id;
            secondOrderId = secondOrder.Id;
            courierId = courier.Id;


            var firstResult = await assignmentService
                .AssignCourierAsync(firstOrderId, courierId);
            Assert.Equal(AssignmentOutcome.Success, firstResult.Outcome);

            var secondResult = await assignmentService
                .AssignCourierAsync(secondOrderId, courierId);
            Assert.Equal(AssignmentOutcome.CourierIsBusy, secondResult.Outcome);
        }

        // Assert
        await using (var db = new DeliveryFlowDbContext(_options))
        {
            var firstOrder = await db.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == firstOrderId);
            var courier = await db.Couriers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == courierId);

            Assert.NotNull(firstOrder);
            Assert.NotNull(courier);
            Assert.Equal(OrderStatus.Assigned, firstOrder.Status);
            Assert.Equal(CourierStatus.Busy, courier.Status);

            var secondOrder = await db.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == secondOrderId);

            Assert.NotNull(secondOrder);
            Assert.Equal(OrderStatus.Created, secondOrder.Status);
            Assert.Null(secondOrder.CourierId);
        }
    }

    [Fact]
    public async Task AssignCourier_StaleCourierState_AllowsDoubleAssignment()
    {
        // Arrange + Act
        Guid courierId;
        Guid firstOrderId, secondOrderId;

        await using (var db = new DeliveryFlowDbContext(_options))
        {
            var courierService = new CourierService(db);
            var orderService = new OrderService(db);

            var courier = await courierService
                .CreateAsync("Тестовый курьер");
            var firstOrder = await orderService
                .CreateAsync("Тестовый заказ 1", 2500);
            var secondOrder = await orderService
                .CreateAsync("Тестовый заказ 2", 3500);

            courierId = courier.Id;
            firstOrderId = firstOrder.Id;
            secondOrderId = secondOrder.Id;
        }

        await using (var dbA = new DeliveryFlowDbContext(_options))
        await using (var dbB = new DeliveryFlowDbContext(_options))
        {
            var courierFirstContext = await dbA.Couriers
                .FirstOrDefaultAsync(c => c.Id == courierId);
            var courierSecondContext = await dbB.Couriers
                .FirstOrDefaultAsync(c => c.Id == courierId);

            Assert.NotNull(courierFirstContext);
            Assert.NotNull(courierSecondContext);

            var firstOrder = await dbA.Orders
                .FirstOrDefaultAsync(o => o.Id == firstOrderId);
            var secondOrder = await dbB.Orders
                .FirstOrDefaultAsync(o => o.Id == secondOrderId);

            Assert.NotNull(firstOrder);
            Assert.NotNull(secondOrder);


            var orderServiceFirstContext = new OrderService(dbA);
            var courierServiceFirstContext = new CourierService(dbA);
            var assignmentServiceFirstContext =
                new AssignmentService(
                    orderServiceFirstContext, courierServiceFirstContext, dbA);

            await assignmentServiceFirstContext
                .AssignCourierAsync(firstOrderId, courierId);


            var orderServiceSecondContext = new OrderService(dbB);
            var courierServiceSecondContext = new CourierService(dbB);
            var assignmentServiceSecondContext =
                new AssignmentService(
                    orderServiceSecondContext, courierServiceSecondContext, dbB);

            var result = await assignmentServiceSecondContext
                .AssignCourierAsync(secondOrderId, courierId);

            Assert.Equal(AssignmentOutcome.ConcurrentModification, result.Outcome);
        }

        await using (var db = new DeliveryFlowDbContext(_options))
        {
            var firstOrder = await db.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == firstOrderId);
            var secondOrder = await db.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == secondOrderId);
            var courier = await db.Couriers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == courierId);

            Assert.NotNull(firstOrder);
            Assert.NotNull(secondOrder);
            Assert.NotNull(courier);
            Assert.Equal(OrderStatus.Assigned, firstOrder.Status);
            Assert.Equal(OrderStatus.Created, secondOrder.Status);
            Assert.Equal(CourierStatus.Busy, courier.Status);
        }
    }
}