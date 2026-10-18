# InsuranceAI – RAG Chatbot (.NET + Ollama + Qdrant)

A Retrieval-Augmented Generation (RAG) chatbot that answers questions from uploaded insurance PDFs. Answers are grounded in the documents and every response includes its **sources** (document name + page number).

Everything runs **locally and for free**: the LLM and embeddings come from Ollama, vectors are stored in Qdrant (Docker), and chat history lives in SQL Server.

---

## Features

- **PDF upload** with page-wise text extraction (PdfPig)
- **Chunking** with a sliding window (500 characters, 100 overlap) so context is not lost at chunk boundaries
- **Embeddings** generated with `nomic-embed-text` (768 dimensions) via Ollama
- **Semantic search** in Qdrant using cosine similarity
- **Re-ranking**: top candidates from Qdrant are scored by the LLM and the best ones are used as context
- **Document filter**: optionally search inside one specific document only
- **Source attribution**: every answer returns document name and page number
- **Conversation history** saved in SQL Server (multi-turn chat)
- **Document management**: list uploaded documents, delete a document (removed from both SQL Server and Qdrant)
- **Configurable** through `appsettings.json` (Options pattern, no hardcoded URLs or model names)
- **Error handling and logging**: input validation, friendly errors when Ollama/Qdrant is unavailable, `ILogger` logging

---

## Architecture

```
            ┌──────────────┐
 PDF ─────► │ PdfController│
            └──────┬───────┘
                   │ 1. extract text page by page
                   │ 2. split into chunks (500 / 100 overlap)
                   │ 3. create embedding for each chunk (Ollama)
                   ▼
        ┌───────────────────┐        ┌────────────────┐
        │ Qdrant (vectors + │        │ SQL Server     │
        │ payload: content, │        │ chunks + chat  │
        │ document, page)   │        │ history        │
        └─────────▲─────────┘        └────────────────┘
                  │
 Question ─► ChatController
                  │ 1. embed the question (Ollama)
                  │ 2. vector search in Qdrant (top candidates)
                  │ 3. re-rank candidates with the LLM
                  │ 4. build prompt: context + conversation + question
                  │ 5. generate answer (Ollama)
                  ▼
        Answer + sources (document, page)
```

---

## Tech Stack

| Area | Technology |
|---|---|
| Backend | ASP.NET Core Web API (C#) |
| LLM | Ollama – `llama3.2:3b` |
| Embeddings | Ollama – `nomic-embed-text` |
| Vector database | Qdrant (Docker) |
| Relational database | SQL Server with Entity Framework Core |
| PDF parsing | PdfPig |
| API docs | Swagger |

---

## Getting Started

### Prerequisites

- .NET SDK
- SQL Server (local instance)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Ollama](https://ollama.com)

### 1. Start Qdrant

```bash
docker run -d -p 6333:6333 -p 6334:6334 -v qdrant_storage:/qdrant/storage --name my_qdrant qdrant/qdrant
```

Dashboard: http://localhost:6333/dashboard

### 2. Pull the Ollama models

```bash
ollama pull llama3.2:3b
ollama pull nomic-embed-text
```

### 3. Configure the app

Update `appsettings.json` (connection string and, if needed, the other values):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=InsuranceAIDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Qdrant": {
    "Host": "localhost",
    "Port": 6334,
    "CollectionName": "document_chunks",
    "VectorSize": 768
  },
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "ChatModel": "llama3.2:3b",
    "Temperature": 0.2
  },
  "Rag": {
    "TopK": 3,
    "SimilarityThreshold": 0.50,
    "CandidateCount": 10,
    "RerankMinScore": 5
  }
}
```

### 4. Create the database and run

```bash
dotnet ef database update
dotnet run --project InsuranceAI.Api
```

Open Swagger at `https://localhost:<port>/swagger`.

---

## API Overview

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/Pdf/upload` | Upload a PDF; it is chunked, embedded and stored |
| GET | `/api/Pdf/list` | List uploaded documents with chunk and page counts |
| DELETE | `/api/Pdf/delete?documentName=...` | Delete a document from SQL Server and Qdrant |
| POST | `/api/Chat` | Ask a question (optional `documentName` filter) |
| POST | `/api/Chat/clear?conversationId=...` | Clear a conversation's history |

### Example

Request:

```json
{
  "conversationId": "test-1",
  "message": "What does this document say about insurance claims?",
  "documentName": "insurance_rag_test.pdf"
}
```

Response (shortened):

```json
{
  "conversationId": "test-1",
  "question": "What does this document say about insurance claims?",
  "answer": "To file an insurance claim, the policyholder should submit the required documents ...",
  "sources": [
    { "documentName": "insurance_rag_test.pdf", "pageNumber": 1, "similarity": 0.75 }
  ]
}
```

---

## Design Decisions

- **Overlapping chunks:** a sentence that falls on a chunk boundary still appears fully in at least one chunk.
- **Cosine similarity:** compares the direction of vectors, so chunks with the same meaning match even when the wording differs. I first implemented it manually (dot product / magnitudes) to understand it, then replaced it with Qdrant's indexed search for speed.
- **Re-ranking:** vector search is fast but approximate. Fetching more candidates and letting the LLM judge each one improves accuracy, at the cost of latency (one extra LLM call per candidate).
- **Score threshold:** if nothing is relevant enough, the bot answers "I could not find the answer in the provided document." instead of guessing.
- **Options pattern:** URLs, model names and thresholds live in configuration, not in code.

---

## Known Limitations and Future Work

- Re-ranking calls the LLM sequentially, so responses are slower; parallel calls or a dedicated cross-encoder model would help.
- Chunking is character-based; sentence- or paragraph-aware chunking would keep context cleaner.
- Hybrid search (semantic + keyword) for exact IDs and policy numbers.
- No authentication yet.
- Evaluation of answer quality (precision/recall style metrics).

---

## What I Learned

Built end to end to understand RAG from the inside: sliding-window chunking, embeddings, cosine similarity, vector databases, prompt construction, re-ranking, configuration management, error handling, and Git workflow (branches and pull requests).