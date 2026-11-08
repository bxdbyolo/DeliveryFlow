using DeliveryFlow.Api.Models;

namespace DeliveryFlow.Api.DTOs;

public record CourierResponse(
    Guid Id, string Name, CourierStatus Status);