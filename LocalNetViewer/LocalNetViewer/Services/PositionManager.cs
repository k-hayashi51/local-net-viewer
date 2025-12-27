using LocalNetViewer.Constants;
using LocalNetViewer.Models;

namespace LocalNetViewer.Services
{
    public static class PositionManager
    {
        private readonly static EnumerationOptions options = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = false
        };


        public static List<FileInfoViewModel> GetChildDirectoryInfoByPosition(string position = "")
        {
            // ---------- ① ドライブ一覧 ----------
            if (string.IsNullOrEmpty(position))
            {
                var drives = DriveInfo.GetDrives();
                var list = new List<FileInfoViewModel>();

                for (int i = 0; i < drives.Length; i++)
                {
                    list.Add(new FileInfoViewModel
                    {
                        Position = (i + 1).ToString(),
                        IsDirectory = true,
                        FileType = FileType.None,
                        Name = drives[i].Name.TrimEnd('\\') // "C:" の形式で返す
                    });
                }

                return list;
            }

            // ---------- ② indexString → パス ----------
            var directoryPath = GetPathByPosition(position);

            var result = new List<FileInfoViewModel>();

            // ---------- ③ 子ディレクトリ ----------
            var dirs = Directory.GetDirectories(directoryPath, "*", options)
                .OrderBy(p => p)
                .ToList();
            for (int i = 0; i < dirs.Count; i++)
            {
                var childDirsCount = Directory.GetDirectories(dirs[i], "*", options).Length;
                var childPosition = $"{position}-{i + 1}";
                result.Add(new FileInfoViewModel
                {
                    Position = childPosition,
                    IsDirectory = true,
                    FileType = FileType.None,
                    Name = Path.GetFileName(dirs[i]),
                    ChildImagePositions = [.. Directory.EnumerateFiles(dirs[i], "*", options)
                        .Select((path, index) => new { path, index })
                        .Where(x => Path.GetExtension(x.path).ToFileType() == FileType.Image)
                        .Take(4)
                        .Select(x => $"{childPosition}-{childDirsCount + x.index + 1}")]
                });
            }

            // ---------- ④ 子ファイル ----------
            var files = Directory.GetFiles(directoryPath, "*", options)
                .OrderBy(p => p)
                .ToList();
            for (int i = 0; i < files.Count; i++)
            {
                result.Add(new FileInfoViewModel
                {
                    Position = $"{position}-{dirs.Count + i + 1}",
                    IsDirectory = false,
                    FileType = Path.GetExtension(files[i]).ToFileType(),
                    Name = Path.GetFileName(files[i])
                });
            }

            return result;
        }

        public static string GetPathByPosition(string position)
        {
            var parts = position
                .Split('-', StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .ToList();

            if (parts.Count == 0)
                throw new ArgumentException("インデックス文字列が不正です。");

            // --- ドライブ選択 ---
            int driveIndex = parts[0] - 1; // 1始まり → 0始まり

            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady)
                .OrderBy(d => d.Name)
                .ToList();

            if (driveIndex < 0 || driveIndex >= drives.Count)
                throw new IndexOutOfRangeException("指定された番号のドライブは存在しません。");

            string currentPath = drives[driveIndex].RootDirectory.FullName;

            // --- 階層を辿る ---
            foreach (var index in parts.Skip(1))
            {
                var entries = Directory.EnumerateDirectories(currentPath, "*", options)
                    .OrderBy(p => p)
                    .Concat(
                        Directory.EnumerateFiles(currentPath, "*", options)
                            .OrderBy(p => p)
                    )
                    .ToList();

                int target = index - 1;

                if (target < 0 || target >= entries.Count)
                    throw new IndexOutOfRangeException(
                        $"{currentPath} 内に {index} 番目の項目は存在しません。");

                currentPath = entries[target];
            }

            return currentPath;
        }
    }
}
