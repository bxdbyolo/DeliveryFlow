using DeliveryFlow.Api.Models;

namespace DeliveryFlow.Api.Services.Assignments;

public record AssignmentResult(
    AssignmentOutcome Outcome,
    Order? Order = null);