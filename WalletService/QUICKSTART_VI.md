# Quick Start Guide - WalletService với VNPay

## Yêu cầu
- Docker & Docker Compose
- VNPay Sandbox Account (https://sandbox.vnpayment.vn/)

## Cài đặt và Chạy

### 1. Cấu hình VNPay
Cập nhật file `WalletService/appsettings.json`:
```json
{
  "VNPay": {
    "TmnCode": "YOUR_TMNCODE_HERE",
    "HashSecret": "YOUR_HASH_SECRET_HERE",
    "ReturnUrl": "http://localhost:5150/api/vnpay/callback",
    "Url": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html"
  }
}
```

### 2. Chạy Services
```bash
cd c:/Users/Tai/Desktop/PRM392
docker-compose up walletservice mongo
```

### 3. Kiểm tra Swagger
Mở trình duyệt: http://localhost:5150/swagger

## Test Flow Hoàn Chỉnh

### Bước 1: Tạo Ví
**Endpoint:** POST `/api/wallets`

**Request Body:**
```json
{
  "userId": "user123",
  "balance": 0
}
```

**Expected Response:** `201 Created`

### Bước 2: Kiểm tra Số Dư Ban Đầu
**Endpoint:** GET `/api/wallets/users/user123`

**Expected Response:**
```json
{
  "id": "...",
  "userId": "user123",
  "balance": 0
}
```

### Bước 3: Tạo URL Thanh Toán VNPay
**Endpoint:** POST `/api/vnpay/create-payment-url`

**Request Body:**
```json
{
  "userId": "user123",
  "amount": 100000,
  "orderInfo": "Nap tien vao vi",
  "returnUrl": "http://localhost:5150/api/vnpay/callback"
}
```

**Expected Response:**
```json
{
  "paymentUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?..."
}
```

### Bước 4: Thanh Toán trên VNPay
1. Copy `paymentUrl` từ response
2. Mở trong trình duyệt
3. Chọn ngân hàng NCB
4. Nhập thông tin thẻ test:
   - **Số thẻ:** 9704198526191432198
   - **Tên chủ thẻ:** NGUYEN VAN A
   - **Ngày hết hạn:** 07/15
   - **Mã OTP:** 123456
5. Xác nhận thanh toán

### Bước 5: Xác Minh Callback Tự Động
Sau khi thanh toán thành công, VNPay sẽ tự động gọi callback endpoint.

**Callback URL:** `GET /api/vnpay/callback?vnp_ResponseCode=00&...`

**Expected Response:**
```json
{
  "success": true,
  "message": "Payment successful and wallet updated",
  "walletBalance": 100000,
  "transactionId": "...",
  "amount": 100000,
  "txnRef": "..."
}
```

### Bước 6: Kiểm tra Số Dư Đã Cập Nhật
**Endpoint:** GET `/api/wallets/users/user123`

**Expected Response:**
```json
{
  "id": "...",
  "userId": "user123",
  "balance": 100000
}
```

### Bước 7: Xem Lịch Sử Giao Dịch
**Endpoint:** GET `/api/transactions/wallets/{walletId}`

**Expected Response:**
```json
[
  {
    "id": "...",
    "walletId": "...",
    "amount": 100000,
    "type": "Deposit",
    "createdAt": "2024-01-15T10:30:00Z",
    "description": "VNPay deposit - TxnRef: 638..., BankCode: NCB, TransactionNo: ..."
  }
]
```

## Kiểm tra qua API Gateway

Nếu muốn test qua Gateway (port 8081):

```bash
# Tạo ví
curl -X POST "http://localhost:8081/api/wallets" \
  -H "Content-Type: application/json" \
  -d '{"userId":"user123","balance":0}'

# Kiểm tra ví
curl -X GET "http://localhost:8081/api/wallets/users/user123"

# Tạo payment URL
curl -X POST "http://localhost:8081/api/vnpay/create-payment-url" \
  -H "Content-Type: application/json" \
  -d '{"userId":"user123","amount":100000,"orderInfo":"Test payment"}'
```

## Mã Lỗi VNPay

| Mã  | Ý nghĩa |
|-----|---------|
| 00  | Giao dịch thành công |
| 07  | Trừ tiền thành công nhưng giao dịch bị nghi ngờ |
| 09  | Thẻ chưa đăng ký dịch vụ |
| 10  | Xác thực sai quá 3 lần |
| 11  | Hết hạn chờ thanh toán |
| 12  | Thẻ bị khóa |
| 24  | Khách hàng hủy giao dịch |
| 51  | Tài khoản không đủ số dư |
| 65  | Vượt quá hạn mức giao dịch |
| 75  | Ngân hàng đang bảo trì |
| 79  | Sai mật khẩu quá số lần quy định |
| 99  | Lỗi không xác định |

## Xử lý Lỗi Thường Gặp

### Lỗi: "Invalid signature"
**Nguyên nhân:** HashSecret không đúng
**Giải pháp:** Kiểm tra lại VNPay:HashSecret trong appsettings.json

### Lỗi: Callback không được gọi
**Nguyên nhân:** ReturnUrl không đúng
**Giải pháp:** Đảm bảo ReturnUrl trỏ đúng đến callback endpoint

### Lỗi: "Cannot determine user ID"
**Nguyên nhân:** UserId không được truyền vào request
**Giải pháp:** Đảm bảo gửi userId trong VNPayRequestDTO

### Lỗi: Số tiền không khớp
**Nguyên nhân:** VNPay trả về amount * 100
**Giải pháp:** Code đã tự động chia 100 (10000000 → 100000 VND)

## Security Features

✅ HMACSHA512 signature verification
✅ Payment amount validation
✅ User identification via orderInfo
✅ Response code checking (00 = success)
✅ Idempotency support (transaction reference)

## Monitoring

Check logs for:
- Payment URL generation
- Callback signature validation
- Wallet updates
- Transaction creation

## Support

Xem thêm:
- `README.md` - Chi tiết API documentation
- `TESTING.md` - Hướng dẫn test chi tiết
- `IMPLEMENTATION_SUMMARY.md` - Tóm tắt implementation

VNPay Sandbox: https://sandbox.vnpayment.vn/
VNPay Docs: https://sandbox.vnpayment.vn/apis/docs/
