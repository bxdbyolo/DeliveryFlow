namespace DeliveryFlow.Api.Services.Assignments;

public enum AssignmentOutcome
{
    Success,
    OrderNotFound,
    CourierNotFound,
    CourierIsBusy,
    OrderAlreadyAssigned,
    OrderDelivered,
    OrderCancelled,
    ConcurrentModification
}