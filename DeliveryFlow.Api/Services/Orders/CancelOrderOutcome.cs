namespace DeliveryFlow.Api.Services.Orders;

public enum CancelOrderOutcome
{
    Success,
    NotFound,
    AlreadyCancelled,
    InvalidState
}