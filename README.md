# 📁 File Manager

A simple, cross-platform file manager with Markdown support, designed for easy LLM integration.

## ✨ Features

- 📂 Browse files and folders
- 📝 Markdown viewer with YAML metadata support
- 🔍 Search by ID, title, tags, or content
- 📤 Drag & drop file upload
- 🏷️ Tag extraction (YAML, hashtags, Obsidian-style)
- 🖨️ Print-friendly markdown view
- 🔌 REST API for LLM integration

## 🚀 Quick Start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Install & Run

```bash
# Clone or download
cd FileManager

# Run (first time will restore packages)
dotnet run

# Or build first
dotnet build
dotnet run
```

Open: http://localhost:5000

### Configure Data Path

```bash
# Linux/macOS
export FILE_MANAGER_DATA_PATH=/path/to/your/data
dotnet run

# Windows
set FILE_MANAGER_DATA_PATH=C:\path\to\your\data
dotnet run
```

## 📄 Markdown Metadata

The app extracts metadata from YAML front matter:

```markdown
---
id: DOC-001
title: My Document
author: John Doe
date: 2024-01-15
tags: [project, notes]
category: work
---

# Content starts here...
```

Supported tag formats:
- YAML: `tags: [tag1, tag2]` or `tags: tag1, tag2`
- Hashtags: `#tag1 #tag2`
- Obsidian: `[[link1]] [[link2]]`

## 🔌 API for LLM Integration

### List Files
```bash
GET /FileManager/ApiList?path=folder
```
Returns: `{ success: true, items: [...] }`

### Read File
```bash
GET /FileManager/ApiRead?path=folder/file.md
```
Returns: `{ success: true, metadata: {...}, content: "...", tags: [...] }`

### Write File
```bash
POST /FileManager/ApiWrite?path=folder/file.md
Content-Type: application/json

{ "content": "---\ntitle: New Doc\n---\n\n# Hello" }
```

### Search
```bash
GET /FileManager/Search?q=searchterm&path=folder
```
Returns: `{ success: true, results: [...] }`

### Download
```bash
GET /FileManager/Download/folder/file.md
```

## 🤖 LLM Integration Examples

### File-based Integration (Simple)
Your LLM reads/writes directly to the data folder:

```python
# Python example
import os

DATA_PATH = os.environ.get('FILE_MANAGER_DATA_PATH', './Data')

# Read a markdown file
with open(f'{DATA_PATH}/notes/meeting.md', 'r') as f:
    content = f.read()

# Write a new file
with open(f'{DATA_PATH}/notes/summary.md', 'w') as f:
    f.write('---\ntitle: Summary\n---\n\n# Meeting Summary\n...')
```

### API Integration (HTTP)

```python
import requests

BASE_URL = 'http://localhost:5000/FileManager'

# List files
resp = requests.get(f'{BASE_URL}/ApiList?path=notes')
files = resp.json()['items']

# Read a file
resp = requests.get(f'{BASE_URL}/ApiRead?path=notes/meeting.md')
data = resp.json()
print(data['metadata']['title'])
print(data['content'])

# Write a file
requests.post(
    f'{BASE_URL}/ApiWrite?path=notes/new.md',
    json={'content': '---\ntitle: New\n---\n\nContent here'}
)

# Search
resp = requests.get(f'{BASE_URL}/Search?q=meeting')
results = resp.json()['results']
```

### Ollama Integration Example

```python
import requests
import ollama

# Read context from file manager
resp = requests.get('http://localhost:5000/FileManager/ApiRead?path=docs/context.md')
context = resp.json()['content']

# Ask Ollama
response = ollama.chat(model='llama3', messages=[
    {'role': 'system', 'content': f'Context:\n{context}'},
    {'role': 'user', 'content': 'Summarize this document'}
])

# Save result
summary = response['message']['content']
requests.post(
    'http://localhost:5000/FileManager/ApiWrite?path=docs/summary.md',
    json={'content': f'---\ntitle: Summary\ndate: 2024-01-15\n---\n\n{summary}'}
)
```

## 📁 Project Structure

```
FileManager/
├── Controllers/
│   └── FileManagerController.cs    # All endpoints
├── Models/
│   └── Models.cs                   # ViewModels
├── Services/
│   └── MarkdownService.cs          # Markdown parsing
├── Views/
│   ├── FileManager/
│   │   ├── Index.cshtml            # File browser
│   │   ├── Markdown.cshtml         # MD viewer
│   │   └── Search.cshtml           # Search results
│   └── Shared/
│       └── _Layout.cshtml          # Layout
├── Data/                           # Default data folder
├── Program.cs                      # Entry point
├── FileManager.csproj              # Project file
└── README.md
```

## 🐧 Running on Linux

```bash
# Install .NET 8
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0

# Add to PATH
export PATH="$HOME/.dotnet:$PATH"

# Run
cd FileManager
dotnet run
```

## 🐳 Docker (Optional)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV FILE_MANAGER_DATA_PATH=/data
EXPOSE 5000
ENTRYPOINT ["dotnet", "FileManager.dll"]
```

```bash
docker build -t filemanager .
docker run -p 5000:5000 -v /your/data:/data filemanager
```

## 📝 License

MIT
