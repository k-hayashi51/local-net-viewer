using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using System.IO;
using System.Reflection.PortableExecutable;

namespace LocalNetViewer.Services
{
    public static class ThumbnailGenerator
    {
        public static byte[] GenerateImageThumbnail(string targetPath)
        {
            using var image = Image.Load(targetPath);

            // 画像そのものを加工する（Clone は不要）
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(200, 0) // 幅200px → 高さは自動
            }));

            using var ms = new MemoryStream();

            // JPEG で保存
            image.Save(ms, new JpegEncoder());

            return ms.ToArray();
        }

        public static byte[] GeneratePdfThumbnail(string targetPath)
        {
            if (!File.Exists(targetPath))
                throw new FileNotFoundException("PDFファイルが見つかりません。", targetPath);

            using var input = PdfReader.Open(targetPath, PdfDocumentOpenMode.Import);

            if (input.Pages.Count == 0)
                throw new InvalidOperationException("PDFにページが存在しません。");

            var output = new PdfDocument();
            output.AddPage(input.Pages[0]);

            using var ms = new MemoryStream();
            output.Save(ms, false);

            return ms.ToArray();
        }
    }
}
