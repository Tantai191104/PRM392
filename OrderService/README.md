# OrderService - Enhanced Version

## 🚀 Features Implemented

### ✅ Authentication & Authorization
- JWT Bearer token authentication
- Role-based authorization
- Users can only view their own orders (unless admin)
- Protected endpoints with `[Authorize]` attribute

### ✅ MongoDB Database Integration
- Replaced in-memory storage with MongoDB
- MongoDB collections and indexes
- Connection string configuration
- Database health checks

### ✅ Enhanced Validation
- FluentValidation for request validation
- Detailed error messages
- Model state validation
- Input sanitization and range checks

### ✅ Structured Logging
- Serilog integration
- Console and file logging
- Structured log format
- Request/response logging
- Error tracking

### ✅ External Service Integration
- Dedicated services for AuthService and ProductService calls
- Proper error handling and logging
- Service interfaces for testability
- HTTP client factory usage

### ✅ Health Checks & Monitoring
- Multiple health check endpoints
- MongoDB connection health
- External service health checks
- Ready/live probes for Kubernetes

## 🏗️ Architecture

```
OrderService/
├── Application/
│   ├── DTOs/           # Data Transfer Objects
│   ├── Services/       # Business logic
│   └── Validators/     # Input validation
├── Domain/
│   └── Entities/       # Domain models
├── Infrastructure/
│   ├── Configuration/  # Settings classes
│   ├── ExternalServices/ # HTTP clients
│   └── Repositories/   # Data access
└── Web/
    └── Controllers/    # API endpoints
```

## 📝 API Endpoints

### Orders
- `GET /api/orders` - Get all orders (Admin only)
- `GET /api/orders/{id}` - Get specific order
- `GET /api/orders/user/{userId}` - Get orders by user
- `POST /api/orders` - Create new order

### Health
- `GET /health` - Basic health check
- `GET /health/simple` - Simple status check

## 🔧 Configuration

### MongoDB Settings
```json
{
  "MongoSettings": {
    "ConnectionString": "mongodb://mongo:27017",
    "DatabaseName": "PRM392OrderDB",
    "OrdersCollectionName": "Orders"
  }
}
```

### JWT Settings
```json
{
  "JwtSettings": {
    "SecretKey": "YOUR_SECRET_KEY",
    "Issuer": "PRM392AuthService",
    "Audience": "PRM392Clients"
  }
}
```

## 🚀 How to Run

### With Docker Compose (Recommended)
```bash
docker-compose up --build
```

### Local Development
1. Start MongoDB: `docker run -d -p 27017:27017 mongo:6.0`
2. Update appsettings.Development.json with local URLs
3. Run: `dotnet run`

## 🧪 Testing

### Manual Testing
Use the `OrderService.http` file with VS Code REST Client extension.

### Authentication Flow
1. Get JWT token from AuthService: `POST /api/auth/login`
2. Use token in Authorization header: `Bearer {token}`
3. Create/view orders with authenticated requests

## 📊 Logging

Logs are written to:
- Console (for Docker)
- File: `logs/orderservice-{date}.log`

Log levels:
- Information: Normal operations
- Warning: Business rule violations
- Error: System errors and exceptions

## 🔄 Future Improvements

### Retry Policies & Circuit Breaker (Planned)
- Add Polly NuGet package for resilience
- Implement retry with exponential backoff
- Circuit breaker for external service calls
- Timeout policies

### Additional Features
- Order status updates
- Inventory management integration
- Payment processing
- Order cancellation
- Notification service integration

## 🐛 Troubleshooting

### Common Issues
1. **MongoDB Connection**: Ensure MongoDB is running and accessible
2. **JWT Validation**: Check secret key matches AuthService
3. **External Services**: Verify AuthService and ProductService are running
4. **Validation Errors**: Check request payload format

### Debug Commands
```bash
# Check service health
curl http://localhost:5139/health

# View logs
docker logs prm392_orderservice

# MongoDB connection test
docker exec -it prm392_mongo mongosh
```

## 📋 Dependencies

### Core Packages
- `Microsoft.AspNetCore.Authentication.JwtBearer` - JWT authentication
- `MongoDB.Driver` - MongoDB integration
- `FluentValidation.AspNetCore` - Input validation
- `Serilog.AspNetCore` - Structured logging

### Future Packages (for complete implementation)
- `Polly` - Resilience patterns
- `AspNetCore.HealthChecks.MongoDb` - MongoDB health checks
- `Microsoft.Extensions.Http.Polly` - HTTP resilience