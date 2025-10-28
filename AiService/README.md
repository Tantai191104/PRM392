# AiService

Microservice AI dự đoán tình trạng pin và đưa ra giá phù hợp dựa trên dữ liệu nhúng.

## Chức năng chính
- Nhận các thông số: phần trăm dung lượng còn lại, điện áp, số chu kỳ sạc, tuổi pin (tháng), nhiệt độ, thương hiệu, dung lượng (mAh), điểm điều kiện vật lý.
- AI dự đoán tình trạng pin (Good/Fair/Poor) và gợi ý giá bán hợp lý.
- Lưu lịch sử dự đoán vào MongoDB; nhận mẫu huấn luyện có nhãn để cải thiện mô hình.
- API RESTful cho các service khác tích hợp.

## Công nghệ
- .NET 8 (C#)
- ML.NET (đa lớp cho tình trạng, hồi quy cho giá)
- MongoDB (lưu lịch sử dự đoán và mẫu huấn luyện)
- Docker-ready

## Cấu hình
AppSettings (appsettings.json):
```
"Mongo": {
	"ConnectionString": "mongodb://localhost:27017",
	"Database": "AiServiceDb",
	"PredictionsCollection": "predictions",
	"TrainingCollection": "training"
}
```

## Endpoints
- POST `/api/BatteryPrediction/predict`: dự đoán tình trạng và giá
- POST `/api/BatteryPrediction/train`: thêm mẫu huấn luyện (cần `labeledStatus` hoặc `labeledPrice`)
- GET `/api/BatteryPrediction/model-info`: thông tin model hiện tại

Xem file `AiService.http` để thử nhanh.