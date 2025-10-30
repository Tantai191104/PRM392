# WalletService - RESTful API Documentation

## Overview
WalletService is a microservice for managing user wallets and VNPay payment integration in the PRM392 e-commerce platform.

## Features
- ✅ RESTful API design with proper HTTP verbs and status codes
- ✅ MongoDB for data persistence
- ✅ VNPay sandbox payment gateway integration
- ✅ Automatic wallet top-up on successful payment
- ✅ Transaction history tracking
- ✅ JWT authentication support (framework ready)
- ✅ Swagger/OpenAPI documentation

## API Endpoints

### Wallet Endpoints
Base URL: `/api/wallets`

#### Get Wallet by User ID
```http
GET /api/wallets/users/{userId}
```
**Response:**
- `200 OK`: Returns wallet details
- `404 Not Found`: Wallet not found for user

#### Create Wallet
```http
POST /api/wallets
```
**Request Body:**
```json
{
  "userId": "string",
  "balance": 0
}
```
**Response:**
- `201 Created`: Wallet created successfully (returns Location header)
- `400 Bad Request`: Invalid wallet data

#### Update Wallet
```http
PUT /api/wallets/{id}
```
**Request Body:**
```json
{
  "id": "string",
  "userId": "string",
  "balance": 100000
}
```
**Response:**
- `204 No Content`: Wallet updated successfully
- `400 Bad Request`: Invalid data or ID mismatch
- `404 Not Found`: Wallet not found

### Transaction Endpoints
Base URL: `/api/transactions`

#### Get Transactions by Wallet ID
```http
GET /api/transactions/wallets/{walletId}
```
**Response:**
- `200 OK`: Returns array of transactions

#### Create Transaction
```http
POST /api/transactions
```
**Request Body:**
```json
{
  "walletId": "string",
  "amount": 50000,
  "type": "Deposit",
  "description": "Top-up from VNPay"
}
```
**Response:**
- `201 Created`: Transaction created successfully (returns Location header)
- `400 Bad Request`: Invalid transaction data

### VNPay Payment Endpoints
Base URL: `/api/vnpay`

#### Create Payment URL
```http
POST /api/vnpay/create-payment-url
```
**Request Body:**
```json
{
  "userId": "user123",
  "amount": 100000,
  "orderInfo": "Nap tien vao vi",
  "returnUrl": "http://localhost:5150/api/vnpay/callback"
}
```
**Response:**
- `200 OK`: Returns VNPay payment URL
```json
{
  "paymentUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?..."
}
```
- `400 Bad Request`: Invalid payment request

#### VNPay Callback (Payment Result)
```http
GET /api/vnpay/callback?vnp_ResponseCode=00&vnp_TxnRef=...&vnp_Amount=...
```
**Query Parameters:** (automatically sent by VNPay)
- `vnp_ResponseCode`: Payment result code ("00" = success)
- `vnp_TxnRef`: Transaction reference
- `vnp_Amount`: Amount * 100 (VND)
- `vnp_SecureHash`: HMACSHA512 signature for verification
- `vnp_BankCode`: Bank code
- `vnp_TransactionNo`: VNPay transaction number
- `vnp_OrderInfo`: Order information (contains userId)

**Response:**
- `200 OK`: Payment processed successfully
```json
{
  "success": true,
  "message": "Payment successful and wallet updated",
  "walletBalance": 150000,
  "transactionId": "trans123",
  "amount": 100000,
  "txnRef": "638..."
}
```
- `400 Bad Request`: Invalid signature or data

## VNPay Integration Flow

### 1. Create Payment URL
User initiates payment → Frontend calls `POST /api/vnpay/create-payment-url` with userId and amount → Backend generates VNPay payment URL with HMACSHA512 signature → Returns payment URL to frontend

### 2. Payment Processing
Frontend redirects user to VNPay payment URL → User completes payment on VNPay → VNPay redirects back to callback URL with payment result

### 3. Callback Handling & Wallet Update
VNPay calls `GET /api/vnpay/callback` with payment result → Backend verifies HMACSHA512 signature → If successful (vnp_ResponseCode="00"):
- Extract userId from vnp_OrderInfo
- Get or create user's wallet
- Add payment amount to wallet balance
- Create transaction record
- Return success response

### 4. Security
- HMACSHA512 signature verification prevents tampering
- OrderInfo contains userId in format "UserId:{userId}|{additionalInfo}"
- All payment data validated before wallet update

## Configuration

### appsettings.json
```json
{
  "MongoDbSettings": {
    "ConnectionString": "mongodb://mongo:27017",
    "DatabaseName": "WalletDB"
  },
  "VNPay": {
    "TmnCode": "YOUR_TMNCODE",
    "HashSecret": "YOUR_SECRET",
    "ReturnUrl": "http://localhost:5150/api/vnpay/callback",
    "Url": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html"
  },
  "JwtSettings": {
    "SecretKey": "supersecretkey1234567890",
    "Issuer": "WalletService",
    "Audience": "WalletApp"
  }
}
```

### VNPay Sandbox Credentials
To use VNPay sandbox:
1. Register at https://sandbox.vnpayment.vn/
2. Get your TmnCode and HashSecret
3. Update appsettings.json with your credentials

## Testing with Swagger

1. Start the service: `dotnet run` or `docker-compose up`
2. Open Swagger UI: http://localhost:5150/swagger
3. Test endpoints:
   - Create wallet for a user
   - Create payment URL with userId
   - Copy payment URL and test in browser
   - Use VNPay test cards (provided in sandbox)
   - Verify callback receives payment and updates wallet
   - Check transaction history

## VNPay Test Cards (Sandbox)
- Bank: NCB
- Card Number: 9704198526191432198
- Card Holder: NGUYEN VAN A
- Expiry Date: 07/15
- OTP: 123456

## Docker Support

### Build
```bash
docker build -t walletservice .
```

### Run with Docker Compose
```bash
docker-compose up walletservice
```

## RESTful API Standards

This service follows RESTful conventions:
- **Resource-based URLs**: `/api/wallets`, `/api/transactions`, `/api/vnpay`
- **HTTP Verbs**: GET (retrieve), POST (create), PUT (update), DELETE (remove)
- **Status Codes**:
  - `200 OK`: Successful GET request
  - `201 Created`: Successful POST with resource creation
  - `204 No Content`: Successful PUT/DELETE
  - `400 Bad Request`: Invalid input data
  - `404 Not Found`: Resource not found
- **DTOs**: All endpoints accept/return DTOs instead of domain entities
- **Validation**: Input validation with detailed error messages
- **Documentation**: ProducesResponseType attributes for OpenAPI

## Architecture

```
WalletService/
├── Domain/
│   └── Entities/          # Domain models (Wallet, Transaction)
├── Application/
│   ├── DTOs/              # Data Transfer Objects
│   └── Services/          # Business logic (WalletAppService, TransactionService)
├── Infrastructure/
│   ├── Repositories/      # Data access (MongoDB)
│   └── VNPay/             # VNPay integration (VNPayService)
├── Web/
│   └── Controllers/       # API endpoints (REST controllers)
└── Program.cs             # Application startup & DI configuration
```

## Dependencies
- MongoDB.Driver 2.22.0
- Swashbuckle.AspNetCore 6.5.0
- Microsoft.AspNetCore.Authentication.JwtBearer 8.0.0
- Microsoft.IdentityModel.Tokens 8.0.0

## Health Check
```http
GET /health
```
Returns service health status.

## Notes
- All amounts in VND (Vietnamese Dong)
- Timestamps in UTC
- MongoDB automatically indexes by Id
- JWT authentication framework is in place but currently commented out for development
