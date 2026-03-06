# PDF Preview API

A C# ASP.NET Web API for uploading and previewing PDF files.

## Features

- Upload PDF files
- Generate PDF previews
- Extract PDF metadata (title, author, page count, etc.)
- Get preview by file ID and page number
- Delete PDF files

## API Endpoints

### 1. Upload PDF
```
POST /api/pdf/upload
Content-Type: multipart/form-data
Body: file (PDF file)
```

### 2. Generate Preview
```
POST /api/pdf/preview
Content-Type: application/json
Body: {
  "filePath": "path/to/file.pdf",
  "pageNumber": 1
}
```

### 3. Get Preview by File ID
```
GET /api/pdf/preview/{fileId}?pageNumber=1
```

### 4. Get PDF Metadata
```
GET /api/pdf/metadata/{fileId}
```

### 5. Delete PDF
```
DELETE /api/pdf/{fileId}
```

## Setup Instructions

### Prerequisites
- .NET 8.0 SDK or later
- Visual Studio 2022 or VS Code

### Installation

1. Restore NuGet packages:
```powershell
dotnet restore
```

2. Build the project:
```powershell
dotnet build
```

3. Run the application:
```powershell
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `https://localhost:5001/swagger`

## Dependencies

- **Microsoft.AspNetCore.OpenApi** - OpenAPI support
- **Swashbuckle.AspNetCore** - Swagger documentation
- **itext7** - PDF processing library
- **itext7.pdfhtml** - PDF to HTML conversion

## Configuration

Edit `appsettings.json` to configure:
- Maximum file size (default: 10MB)
- Allowed file extensions
- Temporary folder location

## Usage Example

### Upload a PDF
```bash
curl -X POST "https://localhost:5001/api/pdf/upload" \
  -H "accept: application/json" \
  -H "Content-Type: multipart/form-data" \
  -F "file=@document.pdf"
```

### Get Preview
```bash
curl -X GET "https://localhost:5001/api/pdf/preview/{fileId}?pageNumber=1" \
  -H "accept: application/json"
```

## Project Structure

```
Larc-project/
├── Controllers/
│   └── PdfController.cs       # API endpoints
├── Models/
│   └── PdfModels.cs          # Request/Response models
├── Services/
│   ├── IPdfService.cs        # Service interface
│   └── PdfService.cs         # PDF processing logic
├── uploads/                   # Uploaded PDF storage
├── Program.cs                 # Application entry point
├── appsettings.json          # Configuration
└── PdfPreviewApi.csproj      # Project file
```

## License

MIT License
