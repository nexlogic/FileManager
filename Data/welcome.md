---
id: SAMPLE-001
title: Welcome to File Manager
author: System
date: 2024-01-15
tags: [welcome, getting-started, sample]
category: documentation
---

# Welcome! 👋

This is a sample markdown document to demonstrate the File Manager features.

## Features

- **File browsing** - Navigate folders and files
- **Markdown rendering** - View `.md` files with full formatting
- **Metadata extraction** - YAML front matter is parsed and displayed
- **Search** - Find files by ID, title, tags, or content
- **LLM-ready** - API endpoints for AI integration

## Metadata Example

This document has the following metadata:
- **ID**: SAMPLE-001
- **Title**: Welcome to File Manager
- **Author**: System
- **Tags**: welcome, getting-started, sample

## Code Example

```python
# Example: Reading this file via API
import requests

resp = requests.get('http://localhost:5000/FileManager/ApiRead?path=welcome.md')
data = resp.json()
print(data['metadata']['title'])
```

## Table Example

| Feature | Status |
|---------|--------|
| File Browser | ✅ Ready |
| Markdown Viewer | ✅ Ready |
| Search | ✅ Ready |
| LLM API | ✅ Ready |

## Tags Demo

You can also use #hashtags inline, or [[Obsidian-style links]].

---

> **Tip**: Try searching for "welcome" or "SAMPLE-001" to see the search feature in action!
