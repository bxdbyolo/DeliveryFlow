using DeliveryFlow.Api.Models;

namespace DeliveryFlow.Api.Services.Deliveries;

public record CompleteDeliveryResult(
    CompleteDeliveryOutcome Outcome,
    Order? Order = null,
    Courier? Courier = null);