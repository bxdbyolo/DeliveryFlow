using System.ComponentModel.DataAnnotations;

namespace DeliveryFlow.Api.Models;

public class Courier(string name)
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = name;
    public CourierStatus Status { get; private set; } = CourierStatus.Available;

    [Timestamp]
    public uint Version { get; private set; }

    public bool TryMarkBusy()
    {
        if (Status is not CourierStatus.Available) return false;

        Status = CourierStatus.Busy;
        return true;
    }

    public bool TryMarkAvailable()
    {
        if (Status is not CourierStatus.Busy) return false;

        Status = CourierStatus.Available;
        return true;
    }
}