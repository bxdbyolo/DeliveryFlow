namespace DeliveryFlow.Api.Services.Orders;

public enum StartDeliveryOutcome
{
    Success,
    NotFound,
    InvalidState,
    ConcurrentModification
}