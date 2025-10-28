# Test Embedding Service Locally (without Docker)

## Prerequisites
```bash
# Install Python dependencies
pip install flask sentence-transformers numpy
```

## Run Embedding Service
```bash
cd c:/Users/Tai/Desktop/PRM392/AiService/PythonEmbedding
python embedding_service.py
```

## Test with curl
```bash
# Health check
curl http://localhost:5555/health

# Generate single embedding
curl -X POST http://localhost:5555/embed \
  -H "Content-Type: application/json" \
  -d '{"text": "Tesla battery 85kWh, excellent condition"}'

# Generate batch embeddings
curl -X POST http://localhost:5555/embed/batch \
  -H "Content-Type: application/json" \
  -d '{"texts": ["Tesla 85kWh", "BYD 60kWh", "LG 70kWh"]}'
```

## Integration with .NET
The AiService will automatically call this Python service at `http://localhost:5555` (or `http://embedding:5555` in Docker).

## Notes
- First run will download ~90MB model from Hugging Face
- Subsequent runs will use cached model
- Model: sentence-transformers/all-MiniLM-L6-v2 (384 dimensions)
- NO external API calls - everything runs locally
