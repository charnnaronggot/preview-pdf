namespace PdfPreviewApi.Models;

public class PdfPreviewRequest
{
    // public string? FilePath { get; set; }
    // public byte[]? FileData { get; set; }
    // public string? FileName { get; set; }
    // public int? PageNumber { get; set; }
    public IFormFile? PdfFile { get; set; }
    public IFormFile? Data { get; set; }
    public short FontSize { get; set; } = 16;
}

public class PdfPreviewResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Base64Image { get; set; }
    public int TotalPages { get; set; }
    public PdfMetadata? Metadata { get; set; }
    public byte[]? PdfData { get; set; }
    public int RowsProcessed { get; set; }

}

public class PdfMetadata
{
    public string? Title { get; set; }
    public string? Author { get; set; }
    public int PageCount { get; set; }
    public DateTime? CreationDate { get; set; }
    public string? FileSize { get; set; }
}

public class PdfUploadResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? FilePath { get; set; }
    public string? FileId { get; set; }
}
