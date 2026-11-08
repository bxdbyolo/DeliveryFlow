using DeliveryFlow.Api.DTOs;
using DeliveryFlow.Api.Services.Assignments;
using DeliveryFlow.Api.Services.Deliveries;
using DeliveryFlow.Api.Services.Orders;
using Microsoft.AspNetCore.Mvc;

namespace DeliveryFlow.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(OrderService orderService,
    AssignmentService assignmentService,
    DeliveryService deliveryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<OrderResponse>>> GetOrders()
    {
        var orders = await orderService.GetAllAsync();

        var orderResponses = orders.Select(
            order => new OrderResponse(
                order.Id, order.Description, order.Price, order.Status, order.CourierId))
            .ToList();

        return Ok(orderResponses);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponse>> GetById(Guid id)
    {
        var order = await orderService.GetByIdAsync(id);

        if (order is null)
            return NotFound();

        var response = new OrderResponse(
            order.Id, order.Description, order.Price, order.Status, order.CourierId);
        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        if (request.Price <= 0)
            return BadRequest("Price must be positive");

        var order = await orderService
            .CreateAsync(request.Description, request.Price);
        var response = new OrderResponse(
            order.Id, order.Description, order.Price, order.Status, order.CourierId);

        return CreatedAtAction(
            nameof(GetById), new { id = order.Id }, response);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<OrderResponse>> CancelOrder(Guid id)
    {
        var (cancelOrderOutcome, order) = await orderService.CancelAsync(id);

        switch (cancelOrderOutcome)
        {
            case CancelOrderOutcome.NotFound:
                return NotFound();
            case CancelOrderOutcome.AlreadyCancelled:
                return Conflict("Order already cancelled");
            case CancelOrderOutcome.InvalidState:
                return Conflict("Invalid state");
            case CancelOrderOutcome.Success:
                {
                    return Ok(new OrderResponse(
                        order!.Id, order.Description, order.Price, order.Status, order.CourierId));
                }
            default:
                throw new InvalidOperationException("Unknown CancelOrderOutcome");
        }
    }

    [HttpPost("{orderId:guid}/assign")]
    public async Task<ActionResult<OrderResponse>> AssignCourier(Guid orderId, [FromBody] AssignCourierRequest request)
    {
        var (outcome, order) = await assignmentService
            .AssignCourierAsync(orderId, request.CourierId);

        if (order is not null && outcome is AssignmentOutcome.Success)
        {
            return Ok(new OrderResponse(
                order.Id, order.Description, order.Price, order.Status, order.CourierId));
        }

        return outcome switch
        {
            AssignmentOutcome.OrderNotFound => NotFound("Order not found"),

            AssignmentOutcome.CourierNotFound => NotFound("Courier not found"),

            AssignmentOutcome.CourierIsBusy => Conflict("Courier is busy"),

            AssignmentOutcome.OrderAlreadyAssigned => Conflict("Order is already assigned"),

            AssignmentOutcome.OrderDelivered => Conflict("Order is already delivered"),

            AssignmentOutcome.OrderCancelled => Conflict("Order is cancelled"),

            AssignmentOutcome.ConcurrentModification =>
                Conflict("Order or courier was modified concurrently"),

            AssignmentOutcome.Success =>
                throw new InvalidOperationException("Order is null, though success"),

            _ => throw new InvalidOperationException("Unknown AssignmentOutcome")
        };
    }

    [HttpPost("{orderId:guid}/start-delivery")]
    public async Task<ActionResult<OrderResponse>> StartDelivery(Guid orderId)
    {
        var (outcome, order) = await orderService.StartDeliveryAsync(orderId);

        if (outcome is StartDeliveryOutcome.Success && order is not null)
            return Ok(new OrderResponse(
                order.Id, order.Description, order.Price,
                order.Status, order.CourierId));

        return outcome switch
        {
            StartDeliveryOutcome.NotFound => NotFound("Order not found"),

            StartDeliveryOutcome.InvalidState =>
                Conflict("Order is in invalid state"),

            StartDeliveryOutcome.ConcurrentModification =>
                Conflict("Order was modified concurrently"),

            StartDeliveryOutcome.Success =>
                throw new InvalidOperationException("Order is null, though success"),

            _ => throw new InvalidOperationException("Unknown StartDeliveryOutcome")
        };
    }

    [HttpPost("{orderId:guid}/complete-delivery")]
    public async Task<ActionResult<OrderResponse>> CompleteDelivery(Guid orderId)
    {
        var (outcome, order, courier) = await deliveryService
            .CompleteDeliveryAsync(orderId);

        if (outcome is CompleteDeliveryOutcome.Success &&
            order is not null && courier is not null)
            return Ok(new OrderResponse(
                order.Id, order.Description, order.Price, order.Status, order.CourierId));

        return outcome switch
        {
            CompleteDeliveryOutcome.OrderNotFound => NotFound("Order not found"),
            CompleteDeliveryOutcome.CourierNotFound => NotFound("Courier not found"),
            CompleteDeliveryOutcome.InvalidState =>
                Conflict("Order is not in delivery or courier is not busy"),
            CompleteDeliveryOutcome.ConcurrentModification =>
                Conflict("Order or courier was modified concurrently"),
            CompleteDeliveryOutcome.Success =>
                throw new InvalidOperationException("Order is null, though success"),
            _ => throw new InvalidOperationException("Unknown CompleteDeliveryOutcome")
        };
    }
}