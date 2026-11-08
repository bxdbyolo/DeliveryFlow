using DeliveryFlow.Api.Data;
using DeliveryFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DeliveryFlow.Api.Services.Orders;

public class OrderService(DeliveryFlowDbContext context)
{
    public async Task<List<Models.Order>> GetAllAsync()
    {
        var orders = await context.Orders
            .AsNoTracking()
            .ToListAsync();

        return orders;
    }

    public async Task<Models.Order?> GetByIdAsync(Guid id)
    {
        var order = await context.Orders
            .FirstOrDefaultAsync(o => o.Id == id);

        return order;
    }

    public async Task<Models.Order> CreateAsync(string description, decimal price)
    {
        var order = new Models.Order(description, price);

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        return order;
    }

    public async Task<CancelOrderResult> CancelAsync(Guid orderId)
    {
        var order = await GetByIdAsync(orderId);
        if (order is null)
            return new CancelOrderResult(CancelOrderOutcome.NotFound);

        if (!order.TryCancel())
            return new CancelOrderResult(order.Status switch
            {
                OrderStatus.Cancelled => CancelOrderOutcome.AlreadyCancelled,
                _ => CancelOrderOutcome.InvalidState
            });

        await context.SaveChangesAsync();
        return new CancelOrderResult(CancelOrderOutcome.Success, order);
    }

    public async Task<StartDeliveryResult> StartDeliveryAsync(Guid id)
    {
        var order = await GetByIdAsync(id);
        if (order is null)
            return new StartDeliveryResult(StartDeliveryOutcome.NotFound);

        if (!order.TryStartDelivery())
            return new StartDeliveryResult(StartDeliveryOutcome.InvalidState);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return new StartDeliveryResult(
                StartDeliveryOutcome.ConcurrentModification);
        }

        return new StartDeliveryResult(StartDeliveryOutcome.Success, order);
    }
}