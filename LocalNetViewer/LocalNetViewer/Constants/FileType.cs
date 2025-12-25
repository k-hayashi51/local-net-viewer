namespace LocalNetViewer.Constants
{
    public enum FileType
    {
        None = 0,
        Image = 1,
        Pdf = 2,
        Video = 3,
        Other = 4,
    }

    public static class FileTypeEx
    {
        private static readonly string[] ImageExtensions =
            { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp", ".tiff" };

        private static readonly string[] VideoExtensions =
            { ".mp4", ".webm", ".mov", ".avi", ".mkv", ".wmv", ".m4v" };

        public static FileType ToFileType(this string ext)
        {
            var lowerExt = ext.ToLower();

            if (ImageExtensions.Contains(lowerExt))
            {
                return FileType.Image;
            }

            if (VideoExtensions.Contains(lowerExt))
            {
                return FileType.Video;
            }

            if (lowerExt == ".pdf")
            {
                return FileType.Pdf;
            }

            return FileType.None;
        }
    }
}
