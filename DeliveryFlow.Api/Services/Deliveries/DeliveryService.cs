using DeliveryFlow.Api.Data;
using DeliveryFlow.Api.Models;
using DeliveryFlow.Api.Services.Couriers;
using DeliveryFlow.Api.Services.Orders;
using Microsoft.EntityFrameworkCore;

namespace DeliveryFlow.Api.Services.Deliveries;

public class DeliveryService(
    OrderService orderService,
    CourierService courierService,
    DeliveryFlowDbContext context)
{
    public async Task<CompleteDeliveryResult> CompleteDeliveryAsync(Guid orderId)
    {
        var order = await orderService
            .GetByIdAsync(orderId);
        if (order is null)
            return new CompleteDeliveryResult(CompleteDeliveryOutcome.OrderNotFound);

        if (order.Status is not OrderStatus.InDelivery || order.CourierId is null)
            return new CompleteDeliveryResult(CompleteDeliveryOutcome.InvalidState);

        var courier = await courierService.GetByIdAsync(order.CourierId.Value);
        if (courier is null)
            return new CompleteDeliveryResult(CompleteDeliveryOutcome.CourierNotFound);

        if (courier.Status is not CourierStatus.Busy)
            return new CompleteDeliveryResult(CompleteDeliveryOutcome.InvalidState);


        if (!courier.TryMarkAvailable())
            throw new InvalidOperationException(
                "Courier order complete failed unexpectedly");

        if (!order.TryCompleteDelivery())
            throw new InvalidOperationException(
                "Order complete failed unexpectedly");

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return new CompleteDeliveryResult(
                CompleteDeliveryOutcome.ConcurrentModification);
        }

        return new CompleteDeliveryResult(CompleteDeliveryOutcome.Success, order, courier);
    }
}