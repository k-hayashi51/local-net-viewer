using LocalNetViewer.Models;
using LocalNetViewer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace LocalNetViewer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        [HttpGet]
        public ActionResult<FileInfoViewModel> GetDirectories()
        {
            var reuslt = PositionManager.GetChildDirectoryInfoByPosition();
            return Ok(reuslt);
        }

        [HttpGet("{position}/child")]
        public ActionResult<FileInfoViewModel> GetDirectories(string position)
        {
            var reuslt = PositionManager.GetChildDirectoryInfoByPosition(position);
            return Ok(reuslt);
        }

        [HttpGet("{position}")]
        public IActionResult GetFile(string position)
        {
            var path = PositionManager.GetPathByPosition(position);
            var stream = System.IO.File.OpenRead(path);
            return File(stream, "application/octet-stream");
        }

        [HttpGet("{position}/path")]
        public IActionResult GetFilePath(string position)
        {
            var path = PositionManager.GetPathByPosition(position);
            return Ok(path);
        }

        [HttpGet("{position}/pdf")]
        public IActionResult GetPdf(string position)
        {
            var path = PositionManager.GetPathByPosition(position);
            var fileName = Path.GetFileName(path);
            return PhysicalFile(path, "application/pdf", fileName, enableRangeProcessing: true);
        }

        [HttpGet("{position}/video")]
        public IActionResult GetVideo(string position)
        {
            var path = PositionManager.GetPathByPosition(position);

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(path, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            return PhysicalFile(
                path,
                contentType,
                enableRangeProcessing: true
            );
        }

        [HttpGet("{position}/thumbnail")]
        public IActionResult? GetThumbnail(string position)
        {
            var path = PositionManager.GetPathByPosition(position);
            var bytes = ThumbnailGenerator.GenerateImageThumbnail(path);
            return File(bytes, "image/jpeg");
        }
    }
}
