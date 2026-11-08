using DeliveryFlow.Api.Data;
using DeliveryFlow.Api.Models;
using DeliveryFlow.Api.Services.Couriers;
using DeliveryFlow.Api.Services.Orders;
using Microsoft.EntityFrameworkCore;

namespace DeliveryFlow.Api.Services.Assignments;

public class AssignmentService(
    OrderService orderService,
    CourierService courierService,
    DeliveryFlowDbContext context)
{
    public async Task<AssignmentResult> AssignCourierAsync(
        Guid orderId, Guid courierId)
    {
        var order = await orderService.GetByIdAsync(orderId);
        if (order is null)
            return new AssignmentResult(AssignmentOutcome.OrderNotFound);

        var courier = await courierService.GetByIdAsync(courierId);
        if (courier is null)
            return new AssignmentResult(AssignmentOutcome.CourierNotFound);

        switch (order.Status)
        {
            case OrderStatus.Created:
                break;

            case OrderStatus.Assigned or OrderStatus.InDelivery:
                return new AssignmentResult(AssignmentOutcome.OrderAlreadyAssigned);
            case OrderStatus.Delivered:
                return new AssignmentResult(AssignmentOutcome.OrderDelivered);
            case OrderStatus.Cancelled:
                return new AssignmentResult(AssignmentOutcome.OrderCancelled);

            default:
                throw new InvalidOperationException("Unknown order status");
        }

        if (courier.Status is not CourierStatus.Available)
            return new AssignmentResult(AssignmentOutcome.CourierIsBusy);


        if (!order.TryAssignTo(courier.Id))
            throw new InvalidOperationException(
                "Order assignment failed unexpectedly");

        if (!courier.TryMarkBusy())
            throw new InvalidOperationException(
                "Courier status change failed unexpectedly");

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return new AssignmentResult(AssignmentOutcome.ConcurrentModification);
        }

        return new AssignmentResult(AssignmentOutcome.Success, order);
    }
}