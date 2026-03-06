using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Utils;
using iText.Kernel.Font;
using iText.IO.Font;
using iText.Forms;
using PdfPreviewApi.Models;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace PdfPreviewApi.Services;

public class PdfService : IPdfService
{
    private readonly ILogger<PdfService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _uploadFolder;
    private readonly Dictionary<string, string> _fileStore = new();

    public PdfService(ILogger<PdfService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        
        // Ensure upload directory exists
        if (!Directory.Exists(_uploadFolder))
        {
            Directory.CreateDirectory(_uploadFolder);
        }
    }

    public async Task<PdfUploadResponse> UploadPdfAsync(IFormFile file)
    {
        try
        {
            // Validate file
            if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return new PdfUploadResponse
                {
                    Success = false,
                    Message = "Only PDF files are allowed"
                };
            }

            var maxSize = _configuration.GetValue<int>("PdfSettings:MaxFileSizeMB", 10) * 1024 * 1024;
            if (file.Length > maxSize)
            {
                return new PdfUploadResponse
                {
                    Success = false,
                    Message = $"File size exceeds maximum allowed size"
                };
            }

            // Generate unique file ID and path
            var fileId = Guid.NewGuid().ToString();
            var fileName = $"{fileId}_{file.FileName}";
            var filePath = Path.Combine(_uploadFolder, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Store file mapping
            _fileStore[fileId] = filePath;

            return new PdfUploadResponse
            {
                Success = true,
                Message = "File uploaded successfully",
                FilePath = filePath,
                FileId = fileId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading PDF");
            return new PdfUploadResponse
            {
                Success = false,
                Message = $"Upload failed: {ex.Message}"
            };
        }
    }

    // public async Task<PdfPreviewResponse> GeneratePreviewAsync(PdfPreviewRequest request)
    // {
    //     try
    //     {
    //         byte[] pdfBytes;

    //         if (request.FileData != null)
    //         {
    //             pdfBytes = request.FileData;
    //         }
    //         else if (!string.IsNullOrEmpty(request.FilePath) && File.Exists(request.FilePath))
    //         {
    //             pdfBytes = await File.ReadAllBytesAsync(request.FilePath);
    //         }
    //         else
    //         {
    //             return new PdfPreviewResponse
    //             {
    //                 Success = false,
    //                 Message = "No valid PDF source provided"
    //             };
    //         }

    //         using var memoryStream = new MemoryStream(pdfBytes);
    //         using var pdfReader = new PdfReader(memoryStream);
    //         using var pdfDocument = new PdfDocument(pdfReader);

    //         var totalPages = pdfDocument.GetNumberOfPages();
    //         var pageNumber = request.PageNumber ?? 1;

    //         if (pageNumber < 1 || pageNumber > totalPages)
    //         {
    //             return new PdfPreviewResponse
    //             {
    //                 Success = false,
    //                 Message = $"Invalid page number. PDF has {totalPages} pages."
    //             };
    //         }

    //         // Get metadata
    //         var metadata = ExtractMetadata(pdfDocument);

    //         // For demonstration, we'll return metadata and a simple response
    //         // In a real implementation, you would render the PDF page to an image
    //         var response = new PdfPreviewResponse
    //         {
    //             Success = true,
    //             Message = "Preview generated successfully",
    //             TotalPages = totalPages,
    //             Metadata = metadata,
    //             Base64Image = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="
    //         };

    //         return response;
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error generating preview");
    //         return new PdfPreviewResponse
    //         {
    //             Success = false,
    //             Message = $"Preview generation failed: {ex.Message}"
    //         };
    //     }
    // }

    // public async Task<PdfPreviewResponse> GeneratePreviewByIdAsync(string fileId, int pageNumber)
    // {
    //     if (!_fileStore.TryGetValue(fileId, out var filePath) || !File.Exists(filePath))
    //     {
    //         return new PdfPreviewResponse
    //         {
    //             Success = false,
    //             Message = "PDF file not found"
    //         };
    //     }

    //     var request = new PdfPreviewRequest
    //     {
    //         FilePath = filePath,
    //         PageNumber = pageNumber
    //     };

    //     return await GeneratePreviewAsync(request);
    // }

    public async Task<PdfMetadata?> GetPdfMetadataAsync(string fileId)
    {
        try
        {
            if (!_fileStore.TryGetValue(fileId, out var filePath) || !File.Exists(filePath))
            {
                return null;
            }

            var pdfBytes = await File.ReadAllBytesAsync(filePath);
            using var memoryStream = new MemoryStream(pdfBytes);
            using var pdfReader = new PdfReader(memoryStream);
            using var pdfDocument = new PdfDocument(pdfReader);

            return ExtractMetadata(pdfDocument);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving metadata");
            return null;
        }
    }

    public async Task<bool> DeletePdfAsync(string fileId)
    {
        try
        {
            if (!_fileStore.TryGetValue(fileId, out var filePath))
            {
                return false;
            }

            if (File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath));
            }

            _fileStore.Remove(fileId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting PDF");
            return false;
        }
    }

    private PdfMetadata ExtractMetadata(PdfDocument pdfDocument)
    {
        var info = pdfDocument.GetDocumentInfo();
        var fileInfo = new FileInfo(_uploadFolder);

        return new PdfMetadata
        {
            Title = info.GetTitle() ?? "Unknown",
            Author = info.GetAuthor() ?? "Unknown",
            PageCount = pdfDocument.GetNumberOfPages(),
            CreationDate = DateTime.Now,
            FileSize = $"{fileInfo.Length / 1024} KB"
        };
    }    public async Task<PdfPreviewResponse> PreviewForm(IFormFile? pdfFile, IFormFile? data, short fontSize = 16)
    {
        try
        {
            if (pdfFile == null || pdfFile.Length == 0)
            {
                return new PdfPreviewResponse { Success = false, Message = "PDF file is required" };
            }

            if (data == null || data.Length == 0)
            {
                return new PdfPreviewResponse { Success = false, Message = "Data file (CSV or JSON) is required" };
            }

            // Read uploaded PDF into memory
            byte[] fileBytes;
            using (MemoryStream uploadStream = new())
            {
                await pdfFile.CopyToAsync(uploadStream);
                fileBytes = uploadStream.ToArray();
            }

            // Read data file content
            string dataContent;
            using (var reader = new StreamReader(data.OpenReadStream(), System.Text.Encoding.UTF8))
            {
                dataContent = await reader.ReadToEndAsync();
            }

            // Parse rows — detect CSV or JSON by file extension or content
            List<Dictionary<string, string>> allRows;
            var fileName = data.FileName?.ToLowerInvariant() ?? "";

            if (fileName.EndsWith(".csv"))
            {
                allRows = ParseCsvContent(dataContent);
            }
            else if (fileName.EndsWith(".json"))
            {
                allRows = ParseDataRows(dataContent);
            }
            else
            {
                // Auto-detect: if starts with [ or { it's JSON, otherwise CSV
                var trimmed = dataContent.TrimStart();
                allRows = trimmed.StartsWith('[') || trimmed.StartsWith('{')
                    ? ParseDataRows(dataContent)
                    : ParseCsvContent(dataContent);
            }

            if (allRows.Count == 0)
            {
                return new PdfPreviewResponse { Success = false, Message = "No valid data rows found" };
            }

            // Single row
            if (allRows.Count == 1)
            {
                var pdfData = await FillSingleForm(fileBytes, allRows[0], fontSize);
                if (pdfData == null)
                {
                    return new PdfPreviewResponse { Success = false, Message = "Failed to fill PDF form" };
                }
                return new PdfPreviewResponse
                {
                    Success = true,
                    Message = "Preview generated successfully",
                    PdfData = pdfData,
                    RowsProcessed = 1
                };
            }

            // Multiple rows — fill each, then merge
            List<byte[]> filledPdfs = new();
            for (int i = 0; i < allRows.Count; i++)
            {
                var filled = await FillSingleForm(fileBytes, allRows[i], fontSize);
                if (filled == null)
                {
                    return new PdfPreviewResponse
                    {
                        Success = false,
                        Message = $"Failed to fill PDF form for row {i + 1}"
                    };
                }
                filledPdfs.Add(filled);
            }

            byte[] mergedPdf = MergePdfs(filledPdfs);

            return new PdfPreviewResponse
            {
                Success = true,
                Message = $"Preview generated successfully for {allRows.Count} rows",
                PdfData = mergedPdf,
                RowsProcessed = allRows.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PreviewForm failed");
            return new PdfPreviewResponse { Success = false, Message = $"Error: {ex}" };
        }
    }

    private List<Dictionary<string, string>> ParseCsvContent(string content)
    {
        var rows = new List<Dictionary<string, string>>();
        var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
        {
            return rows;
        }

        var headers = ParseCsvLine(lines[0]);

        for (int i = 1; i < lines.Length; i++)
        {
            var values = ParseCsvLine(lines[i]);
            var dict = new Dictionary<string, string>();

            for (int j = 0; j < headers.Length && j < values.Length; j++)
            {
                var key = headers[j].Trim();
                if (!string.IsNullOrEmpty(key))
                {
                    dict[key] = values[j].Trim();
                }
            }

            if (dict.Count > 0)
            {
                rows.Add(dict);
            }
        }

        return rows;
    }


    private string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    // Check for escaped quote ""
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
        }

        fields.Add(current.ToString());
        return fields.ToArray();
    }

    private List<Dictionary<string, string>> ParseDataRows(string data)
    {
        var rows = new List<Dictionary<string, string>>();
        var trimmed = data.Trim();

        // 1) Try standard JSON parse (single object or array)
        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    if (element.ValueKind == JsonValueKind.Object)
                    {
                        rows.Add(JsonElementToDict(element));
                    }
                }
                return rows;
            }
            else if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                rows.Add(JsonElementToDict(doc.RootElement));
                return rows;
            }
        }
        catch (JsonException)
        {
            // Not valid single JSON — try reading as concatenated JSON objects
        }

        // 2) Handle concatenated JSON objects: {"a":"1"}{"b":"2"}{"c":"3"}
        //    Split by matching braces, then parse each chunk individually.
        foreach (var jsonChunk in SplitConcatenatedJsonObjects(trimmed))
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonChunk);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    rows.Add(JsonElementToDict(doc.RootElement));
                }
            }
            catch (JsonException)
            {
                _logger.LogWarning("Skipping invalid JSON chunk: {Chunk}", jsonChunk);
            }
        }

        return rows;
    }

    private static IEnumerable<string> SplitConcatenatedJsonObjects(string input)
    {
        int depth = 0;
        int start = -1;
        bool inString = false;
        bool escape = false;

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            if (escape)
            {
                escape = false;
                continue;
            }

            if (c == '\\' && inString)
            {
                escape = true;
                continue;
            }

            if (c == '"')
            {
                inString = !inString;
                continue;
            }

            if (inString)
                continue;

            if (c == '{')
            {
                if (depth == 0)
                    start = i;
                depth++;
            }
            else if (c == '}')
            {
                depth--;
                if (depth == 0 && start >= 0)
                {
                    yield return input.Substring(start, i - start + 1);
                    start = -1;
                }
            }
        }
    }

    private Dictionary<string, string> JsonElementToDict(JsonElement element)
    {
        var dict = new Dictionary<string, string>();
        foreach (var prop in element.EnumerateObject())
        {
            dict[prop.Name] = prop.Value.ValueKind == JsonValueKind.String
                ? prop.Value.GetString() ?? string.Empty
                : prop.Value.ToString();
        }
        return dict;
    }

    private Task<byte[]?> FillSingleForm(byte[] templateBytes, Dictionary<string, string> fieldSet, short fontSize)
    {
        try
        {
            using MemoryStream outputStream = new();
            using MemoryStream inputStream = new(templateBytes);

            PdfReader reader = new(inputStream);
            PdfWriter writer = new(outputStream);
            writer.SetCloseStream(false);
            reader.SetCloseStream(false);

            PdfDocument pdfDoc;
            try
            {
                pdfDoc = new PdfDocument(reader, writer);
            }
            catch (iText.Kernel.Exceptions.BadPasswordException)
            {
                return Task.FromResult<byte[]?>(null);
            }

            var acroForm = PdfAcroForm.GetAcroForm(pdfDoc, true);
            if (acroForm != null && acroForm.GetXfaForm().IsXfaPresent())
            {
                pdfDoc.Close();
                return Task.FromResult<byte[]?>(null);
            }

            // Load a Thai-compatible font with IDENTITY_H encoding for full Unicode support
            PdfFont thaiFont = LoadThaiFont();

            var acro = PdfAcroForm.GetAcroForm(pdfDoc, true);
            if (acro != null)
            {
                var fields = acro.GetFormFields();
                foreach (var kvp in fieldSet)
                {
                    if (fields.TryGetValue(kvp.Key, out var field))
                    {
                        var fieldType = field.GetFormType();
                        
                        if (fieldType == null || fieldType.Equals(PdfName.Tx) || fieldType.Equals(PdfName.Ch))
                        {
                            // Text field or choice field — use Thai font
                            field.SetFont(thaiFont);
                            field.SetValue(kvp.Value, thaiFont, fontSize);
                        }
                        else
                        {
                            // Checkbox, radio button, push button — set value only (no font override)
                            field.SetValue(kvp.Value);
                        }
                    }
                }
                acro.FlattenFields();
            }

            pdfDoc.Close();
            return Task.FromResult<byte[]?>(outputStream.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FillSingleForm failed");
            return Task.FromResult<byte[]?>(null);
        }
    }

    private PdfFont LoadThaiFont()
    {
        // Look for a .ttf font in the Fonts folder
        var fontsDir = Path.Combine(AppContext.BaseDirectory, "Fonts");
        
        if (Directory.Exists(fontsDir))
        {
            var fontFile = Directory.GetFiles(fontsDir, "*.ttf").FirstOrDefault()
                        ?? Directory.GetFiles(fontsDir, "*.otf").FirstOrDefault();
            
            if (fontFile != null)
            {
                return PdfFontFactory.CreateFont(fontFile, PdfEncodings.IDENTITY_H, 
                    PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
            }
        }

        // Fallback: try common Windows Thai font paths
        string[] fallbackPaths = new[]
        {
            @"C:\Windows\Fonts\THSarabunNew.ttf",
            @"C:\Windows\Fonts\tahoma.ttf",
            @"C:\Windows\Fonts\arial.ttf",
            @"C:\Windows\Fonts\cordia.ttc",
        };

        foreach (var path in fallbackPaths)
        {
            if (File.Exists(path))
            {
                return PdfFontFactory.CreateFont(path, PdfEncodings.IDENTITY_H,
                    PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
            }
        }

        // Last resort: use default (won't support Thai)
        _logger.LogWarning("No Thai-compatible font found. Thai characters may not render.");
        return PdfFontFactory.CreateFont();
    }

    private byte[] MergePdfs(List<byte[]> pdfBytesList)
    {
        using MemoryStream mergedStream = new();
        using PdfWriter writer = new(mergedStream);
        writer.SetCloseStream(false);
        using PdfDocument mergedDoc = new(writer);
        PdfMerger merger = new(mergedDoc);

        foreach (var pdfBytes in pdfBytesList)
        {
            using MemoryStream sourceStream = new(pdfBytes);
            using PdfReader reader = new(sourceStream);
            using PdfDocument sourceDoc = new(reader);
            merger.Merge(sourceDoc, 1, sourceDoc.GetNumberOfPages());
        }

        mergedDoc.Close();
        return mergedStream.ToArray();
    }

    
}
