using Microsoft.AspNetCore.Mvc;
using PdfPreviewApi.Models;
using PdfPreviewApi.Services;

namespace PdfPreviewApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PdfController : ControllerBase
{
    private readonly IPdfService _pdfService;
    private readonly ILogger<PdfController> _logger;

    public PdfController(IPdfService pdfService, ILogger<PdfController> logger)
    {
        _pdfService = pdfService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a PDF file
    /// </summary>
    [HttpPost("upload")]
    public async Task<ActionResult<PdfUploadResponse>> UploadPdf(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new PdfUploadResponse 
                { 
                    Success = false, 
                    Message = "No file uploaded" 
                });
            }

            var result = await _pdfService.UploadPdfAsync(file);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading PDF");
            return StatusCode(500, new PdfUploadResponse 
            { 
                Success = false, 
                Message = $"Error: {ex.Message}" 
            });
        }
    }

    /// <summary>
    /// Get PDF preview as base64 image
    /// </summary>
    // [HttpPost("preview")]
    // public async Task<ActionResult<PdfPreviewResponse>> GetPdfPreview([FromBody] PdfPreviewRequest request)
    // {
    //     try
    //     {
    //         var result = await _pdfService.GeneratePreviewAsync(request);
    //         return Ok(result);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error generating PDF preview");
    //         return StatusCode(500, new PdfPreviewResponse 
    //         { 
    //             Success = false, 
    //             Message = $"Error: {ex.Message}" 
    //         });
    //     }
    // }

    /// <summary>
    /// Get PDF metadata
    /// </summary>
    [HttpGet("metadata/{fileId}")]
    public async Task<ActionResult<PdfMetadata>> GetPdfMetadata(string fileId)
    {
        try
        {
            var metadata = await _pdfService.GetPdfMetadataAsync(fileId);
            if (metadata == null)
            {
                return NotFound(new { Message = "PDF not found" });
            }
            return Ok(metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving PDF metadata");
            return StatusCode(500, new { Message = $"Error: {ex.Message}" });
        }
    }

    /// <summary>
    /// Get PDF preview by file ID
    /// </summary>
    // [HttpGet("preview/{fileId}")]
    // public async Task<ActionResult<PdfPreviewResponse>> GetPdfPreviewById(string fileId, [FromQuery] int pageNumber = 1)
    // {
    //     try
    //     {
    //         var result = await _pdfService.GeneratePreviewByIdAsync(fileId, pageNumber);
    //         if (!result.Success)
    //         {
    //             return NotFound(result);
    //         }
    //         return Ok(result);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error generating PDF preview");
    //         return StatusCode(500, new PdfPreviewResponse 
    //         { 
    //             Success = false, 
    //             Message = $"Error: {ex.Message}" 
    //         });
    //     }
    // }

    /// <summary>
    /// Delete a PDF file
    /// </summary>
    [HttpDelete("{fileId}")]
    public async Task<ActionResult> DeletePdf(string fileId)
    {
        try
        {
            var result = await _pdfService.DeletePdfAsync(fileId);
            if (!result)
            {
                return NotFound(new { Message = "PDF not found" });
            }
            return Ok(new { Message = "PDF deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting PDF");
            return StatusCode(500, new { Message = $"Error: {ex.Message}" });
        }
    }
    [HttpPost("preview-form")]
    public async Task<ActionResult> PreviewForm([FromForm] PdfPreviewRequest request)
    {
        try
        {
            var result = await _pdfService.PreviewForm(request.PdfFile, request.Data, request.FontSize);
                if (!result.Success || result.PdfData == null || result.PdfData.Length == 0)
                    return BadRequest(new PdfPreviewResponse 
                    { 
                        Success = false, 
                        Message = result.Message ?? "Failed to generate PDF preview" 
                    });

                return File(result.PdfData, "application/pdf", "preview.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF form preview");
            return StatusCode(500, new PdfPreviewResponse 
            { 
                Success = false, 
                Message = $"Error: {ex.Message}" 
            });
        }
    }
}
