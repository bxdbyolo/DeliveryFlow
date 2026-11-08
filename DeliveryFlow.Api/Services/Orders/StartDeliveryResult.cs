namespace DeliveryFlow.Api.Services.Orders;

public record StartDeliveryResult(
    StartDeliveryOutcome Outcome,
    Models.Order? Order = null);