using InsuranceAI.Api.Data;
using InsuranceAI.Api.Model;
using InsuranceAI.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using UglyToad.PdfPig;
using Microsoft.EntityFrameworkCore;

namespace InsuranceAI.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PdfController : ControllerBase
    {
        private readonly IPdfService _pdfService;
        private readonly IEmbeddingService _embeddingService;
        private readonly AppDbContext _context;

        private readonly IQdrantService _qdrantService;

        public PdfController(
            IPdfService pdfService,
            IEmbeddingService embeddingService,
            AppDbContext context, IQdrantService qdrantService)
        {
            _pdfService = pdfService;
            _embeddingService = embeddingService;
            _context = context;
            _qdrantService = qdrantService;
        }

        //[HttpPost("upload")]
        //public async Task<IActionResult> UploadPdf(
        //    IFormFile file)
        //{
        //    // 1. Check file
        //    if (file == null || file.Length == 0)
        //    {
        //        return BadRequest(
        //            "Please upload a PDF file."
        //        );
        //    }

        //    // 2. Check extension
        //    if (Path.GetExtension(file.FileName)
        //        .ToLower() != ".pdf")
        //    {
        //        return BadRequest(
        //            "Only PDF files are allowed."
        //        );
        //    }
        //    // 3. Create Uploads folder
        //    var uploadsFolder = Path.Combine(
        //        Directory.GetCurrentDirectory(),
        //        "Uploads"
        //    );

        //    Directory.CreateDirectory(
        //        uploadsFolder
        //    );

        //    // 4. Create file path
        //    var filePath = Path.Combine(
        //        uploadsFolder,
        //        file.FileName
        //    );

        //    // 5. Save uploaded PDF
        //    using (var stream = new FileStream(
        //        filePath,
        //        FileMode.Create))
        //    {
        //        await file.CopyToAsync(stream);
        //    }

        //    // 6. Extract text from PDF
        //    var extractedText =
        //        _pdfService.ExtractText(filePath);

        //    // 7. Check extracted text
        //    if (string.IsNullOrWhiteSpace(
        //        extractedText))
        //    {
        //        return BadRequest(
        //            "No text found in PDF."
        //        );
        //    }

        //    // 8. Create chunks
        //    var chunks =
        //        _pdfService.CreateChunks(
        //            extractedText
        //        );

        //    // 9. Process every chunk
        //    foreach (var chunk in chunks)
        //    {
        //        // Skip empty chunk
        //        if (string.IsNullOrWhiteSpace(
        //            chunk))
        //        {
        //            continue;
        //        }

        //        // 10. Generate embedding
        //        var embedding =
        //            await _embeddingService
        //                .GetEmbedding(chunk);

        //        // 11. Convert embedding to JSON
        //        var embeddingJson =
        //            JsonSerializer.Serialize(
        //                embedding
        //            );

        //        // 12. Create database object
        //        var documentChunk =
        //            new DocumentChunk
        //            {
        //                Content = chunk,
        //                EmbeddingJson = embeddingJson
        //            };

        //        // 13. Add to database context
        //        _context.DocumentChunks.Add(
        //            documentChunk
        //        );
        //    }

        //    // 14. Save all chunks to SQL Server
        //    await _context.SaveChangesAsync();

        //    // 15. Return success
        //    return Ok(new
        //    {
        //        message =
        //            "PDF processed and saved successfully.",

        //        fileName = file.FileName,

        //        totalChunks = chunks.Count
        //    });
        //}



        [HttpPost("upload")]
        public async Task<IActionResult> UploadPdf(IFormFile file)
        {
            // 1. Check file
            if (file == null || file.Length == 0)
            {
                return BadRequest("Please upload a PDF file.");
            }

            // 2. Check extension
            if (Path.GetExtension(file.FileName).ToLower() != ".pdf")
            {
                return BadRequest("Only PDF files are allowed.");
            }

            // 3. Create Uploads folder
            var uploadsFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Uploads");

            Directory.CreateDirectory(uploadsFolder);

            // 4. Save PDF file
            var filePath = Path.Combine(
                uploadsFolder,
                file.FileName);

            using (var stream = new FileStream(
                filePath,
                FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 5. Open PDF
            using var document = PdfDocument.Open(filePath);

            int totalChunks = 0;

            // 6. Read PDF page by page
            foreach (var page in document.GetPages())
            {
                int pageNumber = page.Number;

                string pageText = page.Text;

                if (string.IsNullOrWhiteSpace(pageText))
                    continue;

                // 7. Create chunks for this page
                var chunks = _pdfService.CreateChunks(
                    pageText,
                    500,
                    100);

                // 8. Process every chunk
                //foreach (var chunk in chunks)
                //{
                //    if (string.IsNullOrWhiteSpace(chunk))
                //        continue;

                //    // 9. Generate embedding
                //    var embedding =
                //        await _embeddingService.GetEmbedding(chunk);

                //    // 10. Convert embedding to JSON
                //    var embeddingJson =
                //        JsonSerializer.Serialize(embedding);

                //    // 11. Create DB record
                //    var documentChunk = new DocumentChunk
                //    {
                //        Content = chunk,

                //        EmbeddingJson = embeddingJson,

                //        DocumentName = file.FileName,

                //        PageNumber = pageNumber
                //    };

                //    // 12. Add to database
                //    _context.DocumentChunks.Add(documentChunk);

                //    totalChunks++;
                //}

                foreach (var chunk in chunks)
                {
                    if (string.IsNullOrWhiteSpace(chunk))
                        continue;

                    var embedding = await _embeddingService.GetEmbedding(chunk);

                    // SQL me save karna (conversation/tracking ke liye rakh sakte ho, ya poora hata do)
                    var documentChunk = new DocumentChunk
                    {
                        Content = chunk,
                        EmbeddingJson = JsonSerializer.Serialize(embedding),
                        DocumentName = file.FileName,
                        PageNumber = pageNumber
                    };

                    _context.DocumentChunks.Add(documentChunk);
                    await _context.SaveChangesAsync();   // Id generate hone ke liye pehle save karo

                    // QDRANT ME INSERT — YE NAYA HAI
                    await _qdrantService.UpsertChunkAsync(
                        documentChunk.Id,
                        chunk,
                        embedding,
                        file.FileName,
                        pageNumber
                    );

                    totalChunks++;
                }
            }

            // 13. Save everything to SQL Server
            await _context.SaveChangesAsync();

            // 14. Return result
            return Ok(new
            {
                message = "PDF processed and saved successfully.",

                fileName = file.FileName,

                totalPages = document.NumberOfPages,

                totalChunks = totalChunks
            });
        }


        [HttpGet("list")]
        public async Task<IActionResult> ListDocuments()
        {
            var documents = await _context.DocumentChunks
                .GroupBy(x => x.DocumentName)
                .Select(g => new
                {
                    DocumentName = g.Key,
                    TotalChunks = g.Count(),
                    TotalPages = g.Select(x => x.PageNumber).Distinct().Count()
                })
                .ToListAsync();

            return Ok(documents);
        }


    }
}
