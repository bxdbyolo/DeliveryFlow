# DeliveryFlow API

A REST API for managing delivery orders, courier assignments, and the delivery lifecycle, built with ASP.NET Core and PostgreSQL.

## Tech Stack

- **C# / .NET 10** — application development
- **ASP.NET Core Web API** — REST endpoints
- **Entity Framework Core** — database access and migrations
- **PostgreSQL 18** — persistent storage
- **Docker & Docker Compose** — containerization and local deployment
- **xUnit** — automated testing
- **Testcontainers** — integration tests with a real PostgreSQL database

## Features

- Create and retrieve orders and couriers
- Assign available couriers to orders
- Cancel orders before assignment
- Manage the delivery lifecycle: `Created → Assigned → InDelivery → Delivered`
- Automatically release couriers after completed deliveries
- Validate business rules and reject invalid state transitions
- Detect concurrent modifications using optimistic concurrency control
- Maintain referential integrity between orders and couriers

## Architecture

DeliveryFlow is organized into several layers, separating HTTP handling, business logic, and database access.

**Project structure:**

```text
DeliveryFlow/
├── DeliveryFlow.Api/
│   ├── Controllers/
│   ├── DTOs/
│   ├── Models/
│   ├── Services/
│   │   ├── Assignments/
│   │   ├── Couriers/
│   │   ├── Deliveries/
│   │   └── Orders/
│   └── Data/
│       ├── DeliveryFlowDbContext.cs
│       └── Migrations/
├── DeliveryFlow.Tests/
│   └── Integration/
├── Dockerfile
└── compose.yaml
```

**Key components:**

- **Controllers** expose REST endpoints, validate incoming requests, and translate operation results into HTTP responses.
- **Services** implement application operations. For example, `AssignmentService` coordinates courier assignments, while `DeliveryService` handles delivery completion and courier availability.
- **Models** represent orders and couriers and enforce valid state transitions through methods such as `TryAssignTo()` and `TryCompleteDelivery()`.
- **Data layer** uses EF Core and PostgreSQL for persistence. A shared `DbContext` allows related entity changes to be saved atomically.

The application uses dependency injection to manage services and database contexts.

Optimistic concurrency control is implemented using PostgreSQL's `xmin` system column to detect conflicting updates.

## Getting Started

### Prerequisites

To run the application locally:

- Docker with Docker Compose

To build or run tests outside containers:

- .NET 10 SDK

### Running with Docker Compose

**1. Clone the repository**

```bash
git clone <repository-url>
cd DeliveryFlow
```

**2. Configure environment variables**

Copy the example configuration:

```bash
cp .env.example .env
```

Open `.env` and set your PostgreSQL password:

```dotenv
POSTGRES_PASSWORD=your-local-password
```

**3. Build and start the application**

```bash
docker compose up -d --build
```

Docker Compose starts the API and PostgreSQL containers. In the local development configuration, EF Core migrations are automatically applied during application startup.

**4. Access the API**

The API is available at:

```text
http://localhost:8080
```

For example, retrieve all orders:

```bash
curl http://localhost:8080/api/orders
```

**5. Stop the application**

```bash
docker compose down
```

PostgreSQL data is stored in a persistent Docker volume and survives normal container restarts.

> **Note:** The Docker Compose configuration is intended for local development. It uses the Development environment and automatically applies database migrations.

## API Endpoints

### Orders

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/orders` | Get all orders |
| GET | `/api/orders/{id}` | Get an order by ID |
| POST | `/api/orders` | Create an order |
| POST | `/api/orders/{id}/cancel` | Cancel an order |
| POST | `/api/orders/{id}/assign` | Assign a courier |
| POST | `/api/orders/{id}/start-delivery` | Start delivery |
| POST | `/api/orders/{id}/complete-delivery` | Complete delivery |

### Couriers

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/couriers` | Get all couriers |
| POST | `/api/couriers` | Create a courier |

### Example: Create an Order

**Request**

```http
POST /api/orders
Content-Type: application/json

{
  "description": "Deliver a package",
  "price": 1500
}
```

A successful request returns `201 Created` with the created order.

The API uses standard HTTP responses, including `400 Bad Request`, `404 Not Found`, and `409 Conflict` for invalid operations.

Additional HTTP request examples and scenarios are available in `DeliveryFlow.Api/DeliveryFlow.Api.http` and can be executed using the JetBrains Rider HTTP Client.

## Testing

The project contains integration tests written with **xUnit** and **Testcontainers**.

Tests cover key business operations, including:

- Order creation and persistence
- Successful courier assignment
- Rejection of assignments involving busy couriers
- Detection and handling of concurrent modifications
- Delivery completion and courier availability

Integration tests use temporary PostgreSQL containers and verify persisted changes through separate EF Core database contexts.

**Run all tests:**

```bash
dotnet test
```

Docker must be running for Testcontainers-based integration tests.

## Future Improvements

- Add pagination and filtering for orders
- Introduce structured logging and improved error handling
- Configure CI with GitHub Actions for automated builds and tests