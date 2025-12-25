using LocalNetViewer.Constants;

namespace LocalNetViewer.Models
{
    public class SettingsModel
    {
        public string Position { get; set; } = string.Empty;

        public ImagePageMode ImagePageMode { get; set; } = ImagePageMode.Scroll;
    }
}
