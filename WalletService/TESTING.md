# VNPay Integration Testing Guide

## Prerequisites
- WalletService running on http://localhost:5150
- MongoDB running
- VNPay sandbox credentials configured in appsettings.json

## Test Flow

### Step 1: Create a Wallet
```bash
curl -X POST "http://localhost:5150/api/wallets" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "testuser123",
    "balance": 0
  }'
```

**Expected Response:** `201 Created` with Location header

### Step 2: Check Initial Balance
```bash
curl -X GET "http://localhost:5150/api/wallets/users/testuser123"
```

**Expected Response:**
```json
{
  "id": "...",
  "userId": "testuser123",
  "balance": 0
}
```

### Step 3: Create Payment URL
```bash
curl -X POST "http://localhost:5150/api/vnpay/create-payment-url" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "testuser123",
    "amount": 100000,
    "orderInfo": "Top up wallet",
    "returnUrl": "http://localhost:5150/api/vnpay/callback"
  }'
```

**Expected Response:**
```json
{
  "paymentUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?vnp_Amount=10000000&vnp_Command=pay&..."
}
```

### Step 4: Complete Payment on VNPay
1. Copy the `paymentUrl` from Step 3
2. Open in browser
3. Select NCB bank
4. Use test card:
   - Card Number: `9704198526191432198`
   - Card Holder: `NGUYEN VAN A`
   - Expiry Date: `07/15`
   - OTP: `123456`
5. Complete payment

### Step 5: Verify Callback (Automatic)
VNPay will automatically call the callback URL. You can also manually test:

```bash
# Example callback (replace with actual values from VNPay)
curl -X GET "http://localhost:5150/api/vnpay/callback?vnp_Amount=10000000&vnp_BankCode=NCB&vnp_ResponseCode=00&vnp_TxnRef=638...&vnp_TransactionNo=14234567&vnp_OrderInfo=UserId:testuser123|Top+up+wallet&vnp_SecureHash=..."
```

**Expected Response:**
```json
{
  "success": true,
  "message": "Payment successful and wallet updated",
  "walletBalance": 100000,
  "transactionId": "...",
  "amount": 100000,
  "txnRef": "638..."
}
```

### Step 6: Verify Wallet Balance Updated
```bash
curl -X GET "http://localhost:5150/api/wallets/users/testuser123"
```

**Expected Response:**
```json
{
  "id": "...",
  "userId": "testuser123",
  "balance": 100000
}
```

### Step 7: Check Transaction History
```bash
# Get wallet ID from Step 6, then:
curl -X GET "http://localhost:5150/api/transactions/wallets/{walletId}"
```

**Expected Response:**
```json
[
  {
    "id": "...",
    "walletId": "...",
    "amount": 100000,
    "type": "Deposit",
    "createdAt": "2024-01-15T10:30:00Z",
    "description": "VNPay deposit - TxnRef: 638..., BankCode: NCB, TransactionNo: 14234567"
  }
]
```

## Testing Scenarios

### Scenario 1: Successful Payment
1. Create payment URL with valid amount
2. Complete payment on VNPay with test card
3. Verify wallet balance increased
4. Verify transaction record created

### Scenario 2: Failed Payment
1. Create payment URL
2. Cancel payment on VNPay (don't complete)
3. Callback will have `vnp_ResponseCode != "00"`
4. Verify wallet balance NOT increased

### Scenario 3: Invalid Signature
1. Manually call callback with tampered parameters
2. Verify returns `400 Bad Request` with "Invalid signature"

### Scenario 4: Multiple Payments
1. Create first payment (100,000 VND)
2. Complete payment
3. Verify balance = 100,000
4. Create second payment (50,000 VND)
5. Complete payment
6. Verify balance = 150,000
7. Verify 2 transactions in history

## Swagger UI Testing

1. Open http://localhost:5150/swagger
2. Navigate to VNPay endpoints
3. Click "Try it out" on `POST /api/vnpay/create-payment-url`
4. Enter request body:
   ```json
   {
     "userId": "testuser123",
     "amount": 100000,
     "orderInfo": "Test payment",
     "returnUrl": "http://localhost:5150/api/vnpay/callback"
   }
   ```
5. Click "Execute"
6. Copy the `paymentUrl` from response
7. Open in new browser tab
8. Complete payment
9. Check wallet balance via Swagger

## Common Issues

### Issue: Invalid Signature
**Cause:** Wrong HashSecret in configuration
**Solution:** Verify VNPay:HashSecret matches your sandbox account

### Issue: Callback Not Triggered
**Cause:** Wrong ReturnUrl in configuration
**Solution:** Ensure VNPay:ReturnUrl matches your actual callback endpoint

### Issue: Wallet Not Updated
**Cause:** UserId not properly extracted from orderInfo
**Solution:** Ensure orderInfo contains "UserId:{userId}" format

### Issue: Amount Mismatch
**Cause:** VNPay sends amount * 100
**Solution:** Code divides by 100 automatically (10000000 → 100000 VND)

## Response Codes

| Code | Description |
|------|-------------|
| 00 | Success |
| 07 | Trừ tiền thành công. Giao dịch bị nghi ngờ (liên quan tới lừa đảo, giao dịch bất thường) |
| 09 | Giao dịch không thành công do: Thẻ/Tài khoản chưa đăng ký dịch vụ |
| 10 | Giao dịch không thành công do: Khách hàng xác thực thông tin thẻ/tài khoản không đúng quá 3 lần |
| 11 | Giao dịch không thành công do: Đã hết hạn chờ thanh toán |
| 12 | Giao dịch không thành công do: Thẻ/Tài khoản bị khóa |
| 24 | Giao dịch không thành công do: Khách hàng hủy giao dịch |
| 51 | Giao dịch không thành công do: Tài khoản không đủ số dư |
| 65 | Giao dịch không thành công do: Tài khoản vượt quá hạn mức giao dịch trong ngày |
| 75 | Ngân hàng thanh toán đang bảo trì |
| 79 | Giao dịch không thành công do: KH nhập sai mật khẩu thanh toán quá số lần quy định |
| 99 | Lỗi không xác định |

## Monitoring

Check WalletService logs for:
- Payment URL generation
- Callback signature validation
- Wallet balance updates
- Transaction creation

Example log output:
```
info: WalletService.Web.Controllers.VNPayController[0]
      Payment callback received: ResponseCode=00, TxnRef=638..., Amount=10000000
info: WalletService.Web.Controllers.VNPayController[0]
      Wallet updated: UserId=testuser123, NewBalance=100000, TransactionId=...
```
