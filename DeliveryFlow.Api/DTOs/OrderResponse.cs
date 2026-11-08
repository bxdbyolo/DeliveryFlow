using DeliveryFlow.Api.Models;

namespace DeliveryFlow.Api.DTOs;

public record OrderResponse(
    Guid Id,
    string Description,
    decimal Price,
    OrderStatus Status,
    Guid? CourierId);