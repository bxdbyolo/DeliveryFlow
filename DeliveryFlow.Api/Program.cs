using DeliveryFlow.Api.Data;
using DeliveryFlow.Api.Services.Assignments;
using DeliveryFlow.Api.Services.Couriers;
using DeliveryFlow.Api.Services.Deliveries;
using DeliveryFlow.Api.Services.Orders;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<CourierService>();
builder.Services.AddScoped<AssignmentService>();
builder.Services.AddScoped<DeliveryService>();

builder.Services.AddDbContext<DeliveryFlowDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DeliveryFlow")));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure migrations
if (app.Environment.IsDevelopment() &&
    app.Configuration.GetValue<bool>("ApplyMigrations"))
{
    await using var scope = app.Services.CreateAsyncScope();

    var db = scope.ServiceProvider
        .GetRequiredService<DeliveryFlowDbContext>();

    await db.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
