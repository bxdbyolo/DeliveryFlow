namespace DeliveryFlow.Api.Services.Deliveries;

public enum CompleteDeliveryOutcome
{
    Success,
    OrderNotFound,
    CourierNotFound,
    InvalidState,
    ConcurrentModification
}