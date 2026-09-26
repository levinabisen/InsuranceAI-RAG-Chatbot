using InsuranceAI.Api.Model;
using UglyToad.PdfPig;

namespace InsuranceAI.Api.Services
{
    public class PdfService: IPdfService
    {
        public string ExtractText(string filePath)
        {
            using var document = PdfDocument.Open(filePath);

            var text = "";

            foreach (var page in document.GetPages())
            {
                text += page.Text + "\n";
            }

            return text;
        }

        public List<PdfPageText> ExtractPages(string filePath)
        {
            using var document = PdfDocument.Open(filePath);

            var pages = new List<PdfPageText>();

            foreach (var page in document.GetPages())
            {
                pages.Add(new PdfPageText
                {
                    PageNumber = page.Number,
                    Text = page.Text
                });
            }

            return pages;
        }
        //public List<string> CreateChunks(
        //    string text,
        //    int chunkSize = 500)
        //{
        //    var chunks = new List<string>();

        //    for (int i = 0; i < text.Length; i += chunkSize)
        //    {
        //        var length = Math.Min(
        //            chunkSize,
        //            text.Length - i
        //        );

        //        var chunk = text.Substring(
        //            i,
        //            length
        //        );

        //        chunks.Add(chunk);
        //    }

        //    return chunks;
        //}

        public List<string> CreateChunks(
        string text,
        int chunkSize = 500,
        int overlap = 100)
        {
            var chunks = new List<string>();

            if (string.IsNullOrWhiteSpace(text))
                return chunks;

            int start = 0;

            while (start < text.Length)
            {
                int length = Math.Min(
                    chunkSize,
                    text.Length - start);

                var chunk = text.Substring(start, length);

                chunks.Add(chunk);

                if (start + length >= text.Length)
                    break;

                start += chunkSize - overlap;
            }

            return chunks;
        }
    }
}
