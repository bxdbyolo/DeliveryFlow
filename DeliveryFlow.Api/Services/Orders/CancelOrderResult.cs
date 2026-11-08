namespace DeliveryFlow.Api.Services.Orders;

public record CancelOrderResult(
    CancelOrderOutcome Outcome,
    Models.Order? Order = null);