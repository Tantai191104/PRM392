Quick start (Docker Compose)

1. Build and run all services:

```bash
cd /c/Users/Tai/Desktop/PRM392
docker compose up --build
```

2. Services:
- API Gateway: http://localhost:5016
- AuthService: http://localhost:5133
- ProductService: http://localhost:5137
- MongoDB: mongodb://localhost:27017

Public API routes (exposed by the API Gateway):
- Auth service (proxied): http://localhost:5016/api/auth/{...}
- Product service (proxied): http://localhost:5016/api/products/{...}

Combined Swagger UI (gateway): http://localhost:5016/swagger

Notes:
- The compose file wires services via internal Docker DNS names `authservice` and `productservice`. The gateway's `Microservices` env uses these names.
- Replace `YOUR_VERY_LONG_SECRET_KEY_HERE` with a secure key in production.
- If you change ports in project launchSettings, update the Dockerfiles and compose ports accordingly.
