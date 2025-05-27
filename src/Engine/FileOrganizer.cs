using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core;

namespace Engine
{
    // Main class that handles all file organization operations
    public class FileOrganizer
    {
        private List<FileCategory> _categories;

        // Events for real-time feedback
        public event EventHandler<ProgressEventArgs> ProgressChanged;
        public event EventHandler<string> LogMessage;

        public FileOrganizer()
        {
            _categories = FileCategory.GetDefaultCategories();
        }

        // Analyze a folder and return preview of what would happen (doesn't move files)
        public async Task<OrganizationPreview> AnalyzeFolder(string folderPath)
        {
            var preview = new OrganizationPreview { SourceFolder = folderPath };

            if (!Directory.Exists(folderPath))
            {
                preview.ErrorMessage = "Folder not found!";
                return preview;
            }

            LogMessage?.Invoke(this, $"🔍 Analyzing folder: {folderPath}");

            try
            {
                // Get all files in the folder (not subdirectories)
                var files = Directory.GetFiles(folderPath, "*", SearchOption.TopDirectoryOnly)
                    .Where(f => !IsSystemFile(f))
                    .ToArray();

                preview.TotalFiles = files.Length;
                preview.TotalSizeBytes = files.Sum(f => new FileInfo(f).Length);

                // Categorize each file
                for (int i = 0; i < files.Length; i++)
                {
                    var category = CategorizeFile(files[i]);
                    var categorizedFile = new CategorizedFile(files[i], category);
                    preview.CategorizedFiles.Add(categorizedFile);

                    // Report progress
                    ProgressChanged?.Invoke(this, new ProgressEventArgs
                    {
                        CurrentFile = categorizedFile.FileName,
                        ProcessedCount = i + 1,
                        TotalCount = files.Length,
                        Stage = "Analyzing"
                    });

                    await Task.Delay(5); // Small delay for smooth progress animation
                }

                // Create summary statistics by category
                preview.CategorySummaries = preview.CategorizedFiles
                    .GroupBy(f => f.Category.Name)
                    .Select(g => new CategorySummary
                    {
                        CategoryName = g.Key,
                        Icon = g.First().Category.Icon,
                        FileCount = g.Count(),
                        TotalSizeBytes = g.Sum(f => f.SizeBytes)
                    })
                    .OrderByDescending(s => s.FileCount)
                    .ToList();

                LogMessage?.Invoke(this, $"✅ Analysis complete: {preview.TotalFiles} files found");
            }
            catch (Exception ex)
            {
                preview.ErrorMessage = $"Error analyzing folder: {ex.Message}";
                LogMessage?.Invoke(this, $"❌ Error: {ex.Message}");
            }

            return preview;
        }

        // Actually organize the files based on the preview
        public async Task<OrganizationResult> OrganizeFiles(OrganizationPreview preview)
        {
            var result = new OrganizationResult();

            if (preview.HasError)
            {
                result.Success = false;
                result.ErrorMessage = preview.ErrorMessage;
                return result;
            }

            LogMessage?.Invoke(this, $"🚀 Starting organization of {preview.TotalFiles} files...");

            try
            {
                var processedCount = 0;

                // Group files by category and process each group
                foreach (var group in preview.CategorizedFiles.GroupBy(f => f.Category))
                {
                    var categoryFolder = Path.Combine(preview.SourceFolder, group.Key.FolderName);

                    // Create category folder if it doesn't exist
                    if (!Directory.Exists(categoryFolder))
                    {
                        Directory.CreateDirectory(categoryFolder);
                        LogMessage?.Invoke(this, $"📁 Created folder: {group.Key.FolderName}");
                    }

                    // Move each file to the category folder
                    foreach (var file in group)
                    {
                        var targetPath = Path.Combine(categoryFolder, file.FileName);

                        // Handle duplicate file names
                        if (File.Exists(targetPath))
                        {
                            targetPath = GetUniqueFileName(targetPath);
                        }

                        // Actually move the file
                        File.Move(file.OriginalPath, targetPath);

                        result.OrganizedFiles.Add(new OrganizedFileRecord
                        {
                            OriginalPath = file.OriginalPath,
                            NewPath = targetPath,
                            Category = file.Category.Name,
                            SizeBytes = file.SizeBytes
                        });

                        processedCount++;

                        // Report progress
                        ProgressChanged?.Invoke(this, new ProgressEventArgs
                        {
                            CurrentFile = file.FileName,
                            ProcessedCount = processedCount,
                            TotalCount = preview.TotalFiles,
                            Stage = "Organizing"
                        });

                        await Task.Delay(10); // Small delay for smooth progress
                    }
                }

                result.Success = true;
                result.TotalFilesOrganized = processedCount;
                result.CategoriesCreated = preview.CategorySummaries.Count;

                LogMessage?.Invoke(this, $"🎉 Organization complete! {processedCount} files organized into {result.CategoriesCreated} categories");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                LogMessage?.Invoke(this, $"❌ Error during organization: {ex.Message}");
            }

            return result;
        }

        // Determine which category a file belongs to based on its extension
        private FileCategory CategorizeFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLower();

            // Check all categories except the last one (which is "Other")
            foreach (var category in _categories.Take(_categories.Count - 1))
            {
                if (category.HandlesExtension(extension))
                    return category;
            }

            // If no category matches, return "Other"
            return _categories.Last();
        }

        // Check if a file is a system file that should be ignored
        private bool IsSystemFile(string filePath)
        {
            var fileName = Path.GetFileName(filePath).ToLower();
            var systemFiles = new[] { "desktop.ini", "thumbs.db", ".ds_store" };
            return systemFiles.Contains(fileName) || fileName.StartsWith(".");
        }

        // Generate a unique filename if a file already exists
        private string GetUniqueFileName(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            var extension = Path.GetExtension(filePath);
            var counter = 1;

            string newPath;
            do
            {
                newPath = Path.Combine(directory, $"{fileNameWithoutExtension} ({counter}){extension}");
                counter++;
            }
            while (File.Exists(newPath));

            return newPath;
        }

        // Allow users to customize categories (for future enhancement)
        public void SetCategories(List<FileCategory> categories)
        {
            _categories = categories ?? throw new ArgumentNullException(nameof(categories));
        }

        public List<FileCategory> GetCategories()
        {
            return new List<FileCategory>(_categories);
        }
    }

    // Represents the preview of what will happen before organizing
    public class OrganizationPreview
    {
        public string SourceFolder { get; set; }
        public int TotalFiles { get; set; }
        public long TotalSizeBytes { get; set; }
        public List<CategorizedFile> CategorizedFiles { get; set; } = new List<CategorizedFile>();
        public List<CategorySummary> CategorySummaries { get; set; } = new List<CategorySummary>();
        public string ErrorMessage { get; set; }
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

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
    }

    // Represents the final result after organizing files
    public class OrganizationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int TotalFilesOrganized { get; set; }
        public int CategoriesCreated { get; set; }
        public List<OrganizedFileRecord> OrganizedFiles { get; set; } = new List<OrganizedFileRecord>();
        public DateTime CompletedAt { get; set; } = DateTime.Now;
    }

    // Record of a single file that was organized
    public class OrganizedFileRecord
    {
        public string OriginalPath { get; set; }
        public string NewPath { get; set; }
        public string Category { get; set; }
        public long SizeBytes { get; set; }

        public override string ToString()
        {
            return $"{Path.GetFileName(OriginalPath)} → {Category}";
        }
    }
}