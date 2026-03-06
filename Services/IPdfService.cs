using PdfPreviewApi.Models;

namespace PdfPreviewApi.Services;

public interface IPdfService
{
    Task<PdfUploadResponse> UploadPdfAsync(IFormFile file);
    // Task<PdfPreviewResponse> GeneratePreviewAsync(PdfPreviewRequest request);
    // Task<PdfPreviewResponse> GeneratePreviewByIdAsync(string fileId, int pageNumber);
    Task<PdfMetadata?> GetPdfMetadataAsync(string fileId);
    Task<bool> DeletePdfAsync(string fileId);
    // Task<byte[]?> GeneratePDF(string fileId, int pageNumber, short fontSize);
    Task<PdfPreviewResponse> PreviewForm(IFormFile pdfFile, IFormFile data, short fontSize);
}
