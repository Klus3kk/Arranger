using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Core
{
    // Represents a file category with its name, folder, and supported extensions
    public class FileCategory
    {
        public string Name { get; set; }
        public string FolderName { get; set; }
        public List<string> Extensions { get; set; }
        public string Icon { get; set; }

        public FileCategory(string name, string folderName, string icon, params string[] extensions)
        {
            Name = name;
            FolderName = folderName;
            Icon = icon;
            Extensions = extensions.ToList();
        }

        // Returns the default smart categories that work out of the box
        public static List<FileCategory> GetDefaultCategories()
        {
            return new List<FileCategory>
            {
                new FileCategory("Documents", "Documents", "📄",
                    ".pdf", ".doc", ".docx", ".txt", ".rtf", ".odt",
                    ".xls", ".xlsx", ".ppt", ".pptx", ".csv"),

                new FileCategory("Images", "Images", "🖼️",
                    ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff",
                    ".svg", ".webp", ".ico", ".psd", ".ai"),

                new FileCategory("Videos", "Videos", "🎬",
                    ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv",
                    ".webm", ".m4v", ".3gp", ".mpg", ".mpeg"),

                new FileCategory("Audio", "Audio", "🎵",
                    ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma",
                    ".m4a", ".opus", ".aiff"),

                new FileCategory("Archives", "Archives", "📦",
                    ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2",
                    ".xz", ".cab", ".iso"),

                new FileCategory("Software", "Software", "⚙️",
                    ".exe", ".msi", ".dmg", ".deb", ".rpm", ".app",
                    ".pkg", ".apk", ".ipa"),

                new FileCategory("Code", "Code", "💻",
                    ".cs", ".js", ".py", ".java", ".cpp", ".c", ".h",
                    ".html", ".css", ".php", ".rb", ".go", ".rs"),

                new FileCategory("Other", "Other", "📁")
            };
        }

        // Check if this category handles the given file extension
        public bool HandlesExtension(string extension)
        {
            return Extensions.Contains(extension.ToLower());
        }

        public override string ToString()
        {
            return $"{Icon} {Name} ({Extensions.Count} types)";
        }
    }

    // Represents a file that has been analyzed and categorized
    public class CategorizedFile
    {
        public string OriginalPath { get; set; }
        public string FileName { get; set; }
        public FileCategory Category { get; set; }
        public long SizeBytes { get; set; }
        public DateTime LastModified { get; set; }

        // Human-readable file size (e.g., "1.5 MB")
        public string SizeFormatted => FormatFileSize(SizeBytes);

        public CategorizedFile(string filePath, FileCategory category)
        {
            OriginalPath = filePath;
            FileName = Path.GetFileName(filePath);
            Category = category;

            if (File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                SizeBytes = fileInfo.Length;
                LastModified = fileInfo.LastWriteTime;
            }
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes == 0) return "0 B";

            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        public override string ToString()
        {
            return $"{FileName} ({SizeFormatted}) → {Category.Name}";
        }
    }

    // Summary information about files in a category
    public class CategorySummary
    {
        public string CategoryName { get; set; }
        public string Icon { get; set; }
        public int FileCount { get; set; }
        public long TotalSizeBytes { get; set; }

        public string TotalSizeFormatted
        {
            get
            {
                if (TotalSizeBytes == 0) return "0 B";

                string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                double len = TotalSizeBytes;
                int order = 0;

                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len = len / 1024;
                }

                return $"{len:0.##} {sizes[order]}";
            }
        }

        public override string ToString()
        {
            return $"{Icon} {CategoryName}: {FileCount} files ({TotalSizeFormatted})";
        }
    }

    // Arguments for progress update events
    public class ProgressEventArgs : EventArgs
    {
        public string CurrentFile { get; set; }
        public int ProcessedCount { get; set; }
        public int TotalCount { get; set; }
        public string Stage { get; set; }
        public double ProgressPercentage => TotalCount > 0 ? (double)ProcessedCount / TotalCount * 100 : 0;

        public override string ToString()
        {
            return $"{Stage}: {ProgressPercentage:F1}% ({ProcessedCount}/{TotalCount})";
        }
    }
}