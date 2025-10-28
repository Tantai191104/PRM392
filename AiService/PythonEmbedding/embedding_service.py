#!/usr/bin/env python3
"""
Local Embedding Service using sentence-transformers
Runs the model locally - no external API calls needed
"""

from flask import Flask, request, jsonify
from sentence_transformers import SentenceTransformer
import numpy as np

app = Flask(__name__)

# Load model once at startup (this will download ~90MB model first time)
print("Loading sentence-transformers model...")
model = SentenceTransformer('sentence-transformers/all-MiniLM-L6-v2')
print("Model loaded successfully!")

@app.route('/health', methods=['GET'])
def health():
    """Health check endpoint"""
    return jsonify({"status": "healthy", "model": "all-MiniLM-L6-v2"})

@app.route('/embed', methods=['POST'])
def embed():
    """
    Generate embedding for input text
    Input: {"text": "battery description..."}
    Output: {"embedding": [0.1, 0.2, ...], "dimensions": 384}
    """
    try:
        data = request.get_json()
        text = data.get('text', '')
        
        if not text:
            return jsonify({"error": "Text is required"}), 400
        
        # Generate embedding
        embedding = model.encode(text)
        
        # Convert numpy array to list for JSON serialization
        embedding_list = embedding.tolist()
        
        return jsonify({
            "embedding": embedding_list,
            "dimensions": len(embedding_list)
        })
    
    except Exception as e:
        return jsonify({"error": str(e)}), 500

@app.route('/embed/batch', methods=['POST'])
def embed_batch():
    """
    Generate embeddings for multiple texts
    Input: {"texts": ["text1", "text2", ...]}
    Output: {"embeddings": [[...], [...]], "dimensions": 384}
    """
    try:
        data = request.get_json()
        texts = data.get('texts', [])
        
        if not texts:
            return jsonify({"error": "Texts array is required"}), 400
        
        # Generate embeddings
        embeddings = model.encode(texts)
        
        # Convert to list
        embeddings_list = [emb.tolist() for emb in embeddings]
        
        return jsonify({
            "embeddings": embeddings_list,
            "dimensions": len(embeddings_list[0]) if embeddings_list else 0,
            "count": len(embeddings_list)
        })
    
    except Exception as e:
        return jsonify({"error": str(e)}), 500

if __name__ == '__main__':
    # Run on port 5555 (internal service)
    app.run(host='0.0.0.0', port=5555, debug=False)
