using System.ComponentModel.DataAnnotations;

namespace DeliveryFlow.Api.Models;

public class Order(
    string description,
    decimal price)
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Description { get; private set; } = description;
    public decimal Price { get; private set; } = price;
    public OrderStatus Status { get; private set; } = OrderStatus.Created;
    public Guid? CourierId { get; private set; }

    [Timestamp]
    public uint Version { get; private set; }

    public bool TryCancel()
    {
        if (Status != OrderStatus.Created)
            return false;

        Status = OrderStatus.Cancelled;
        return true;
    }

    public bool TryAssignTo(Guid courierId)
    {
        if (Status is not OrderStatus.Created ||
            CourierId is not null ||
            courierId == Guid.Empty) return false;

        Status = OrderStatus.Assigned;
        CourierId = courierId;

        return true;
    }

    public bool TryStartDelivery()
    {
        if (Status is not OrderStatus.Assigned || CourierId is null)
            return false;


        Status = OrderStatus.InDelivery;
        return true;
    }

    public bool TryCompleteDelivery()
    {
        if (Status is not OrderStatus.InDelivery || CourierId is null)
            return false;

        Status = OrderStatus.Delivered;
        return true;
    }
}
