using System;
using System.IO;
using System.Threading.Tasks;
using Core;
using Engine;

namespace Arranger
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("--- Arranger ---");
            var organizer = new FileOrganizer();

            // Subscribe to events for real-time feedback
            organizer.ProgressChanged += (sender, e) =>
            {
                Console.Write($"\r⚡ {e.Stage}: {e.ProgressPercentage:F1}% ({e.ProcessedCount}/{e.TotalCount}) - {e.CurrentFile}");
            };

            organizer.LogMessage += (sender, message) =>
            {
                Console.WriteLine($"\n{message}");
            };

            // Get folder from user
            Console.Write("Enter folder path to organize (or press Enter for Downloads): ");
            var folderPath = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(folderPath))
            {
                folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            }

            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine("Folder not found!");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                return;
            }

            // Step 1: Analyze folder (preview mode)
            Console.WriteLine("\nAnalyzing folder...\n");
            var preview = await organizer.AnalyzeFolder(folderPath);

            if (preview.HasError)
            {
                Console.WriteLine($"Error: {preview.ErrorMessage}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            // Show preview results
            Console.WriteLine($"\nPreview Results for: {folderPath}");
            Console.WriteLine("════════════════════════════════════════");
            Console.WriteLine($"Total files: {preview.TotalFiles}");
            Console.WriteLine($"Total size: {preview.TotalSizeFormatted}");
            Console.WriteLine();

            if (preview.CategorySummaries.Count == 0)
            {
                Console.WriteLine("No files found to organize!");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            // Show category breakdown
            foreach (var category in preview.CategorySummaries)
            {
                Console.WriteLine($"{category.Icon} {category.CategoryName}: {category.FileCount} files ({category.TotalSizeFormatted})");
            }

            // Ask for confirmation
            Console.WriteLine("\n" + new string('═', 50));
            Console.Write("Proceed with organization? (y/N): ");
            var confirm = Console.ReadLine();

            if (confirm?.ToLower() != "y")
            {
                Console.WriteLine("Organization cancelled.");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            // Step 2: Actually organize the files
            Console.WriteLine("\nOrganizing files...\n");
            var result = await organizer.OrganizeFiles(preview);

            // Show final results
            Console.WriteLine($"\n\n{(result.Success ? "🎉" : "❌")} Organization Complete!\n");
            

            if (result.Success)
            {
                Console.WriteLine($"✅ {result.TotalFilesOrganized} files organized successfully");
                Console.WriteLine($"📁 {result.CategoriesCreated} categories created");
                Console.WriteLine($"⏰ Completed at: {result.CompletedAt:HH:mm:ss}");

                Console.WriteLine("\nFiles have been organized into folders:");
                foreach (var category in preview.CategorySummaries)
                {
                    if (category.FileCount > 0)
                    {
                        Console.WriteLine($"   {category.Icon} {category.CategoryName}/ - {category.FileCount} files");
                    }
                }
            }
            else
            {
                Console.WriteLine($"Error: {result.ErrorMessage}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}