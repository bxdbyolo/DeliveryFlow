using DeliveryFlow.Api.Data;
using DeliveryFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DeliveryFlow.Api.Services.Couriers;

public class CourierService(DeliveryFlowDbContext context)
{
    public async Task<List<Courier>> GetAllAsync()
    {
        var couriers = await context.Couriers
            .AsNoTracking()
            .ToListAsync();

        return couriers;
    }

    public async Task<Courier?> GetByIdAsync(Guid id)
    {
        var courier = await context.Couriers
            .FirstOrDefaultAsync(x => x.Id == id);

        return courier;
    }

    public async Task<Courier> CreateAsync(string name)
    {
        var courier = new Courier(name);

        context.Couriers.Add(courier);
        await context.SaveChangesAsync();

        return courier;
    }
}