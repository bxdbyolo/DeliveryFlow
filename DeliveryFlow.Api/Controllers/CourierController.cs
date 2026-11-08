using DeliveryFlow.Api.DTOs;
using DeliveryFlow.Api.Services;
using DeliveryFlow.Api.Services.Couriers;
using Microsoft.AspNetCore.Mvc;

namespace DeliveryFlow.Api.Controllers;

[ApiController]
[Route("api/couriers")]
public class CourierController(CourierService courierService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CourierResponse>>> GetCouriers()
    {
        var couriers = await courierService.GetAllAsync();

        var courierResponses = couriers.Select(
            c => new CourierResponse(c.Id, c.Name, c.Status))
            .ToList();

        return Ok(courierResponses);
    }

    [HttpPost]
    public async Task<ActionResult<CourierResponse>> CreateCourier(
        [FromBody] CreateCourierRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest();

        var courier = await courierService.CreateAsync(request.Name);
        var response = new CourierResponse(
            courier.Id, courier.Name, courier.Status);
        return StatusCode(StatusCodes.Status201Created, response);
    }
}