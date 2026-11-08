namespace DeliveryFlow.Api.DTOs;

public record CreateOrderRequest(
    string Description,
    decimal Price);