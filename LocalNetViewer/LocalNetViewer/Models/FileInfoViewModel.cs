using LocalNetViewer.Constants;

namespace LocalNetViewer.Models
{
    public class FileInfoViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public bool IsDirectory { get; set; } = false;
        public FileType FileType { get; set; } = FileType.None;
        public string[] ChildImagePositions { get; set; } = [];
    }
}
