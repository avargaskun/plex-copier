using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace tst
{
    public static class TestFiles
    {
        public static readonly string[] SingleSeries = new[]
        {
            Path.Combine("Single_Series", "[TEST] Single - 01.mkv"),
            Path.Combine("Single_Series", "[TEST] Single - 02.mkv"),
            Path.Combine("Single_Series", "[TEST] Single - 03.mkv"),
        };

        public static readonly string[] DoubleSeries = new[]
        {
            Path.Combine("Double_Series", "Season-01", "[TEST] Double S1 - 01.mkv"),
            Path.Combine("Double_Series", "Season-01", "[TEST] Double S1 - 02.mkv"),
            Path.Combine("Double_Series", "Season-01", "[TEST] Double S1 - 03.mkv"),

            Path.Combine("Double_Series", "Season-02", "[TEST] Double S2 - 01.mkv"),
            Path.Combine("Double_Series", "Season-02", "[TEST] Double S2 - 02.mkv"),
            Path.Combine("Double_Series", "Season-02", "[TEST] Double S2 - 03.mkv"),
        };

        public static readonly string[] LongSeries = new[]
        {
            Path.Combine("Long_Series", "[TEST] Long - 01.mp4"),
            Path.Combine("Long_Series", "[TEST] Long - 02.mp4"),
            Path.Combine("Long_Series", "[TEST] Long - 03.mp4"),
            Path.Combine("Long_Series", "[TEST] Long - 04.mp4"),
            Path.Combine("Long_Series", "[TEST] Long - 05.mp4"),
            Path.Combine("Long_Series", "[TEST] Long - 06.mp4"),
        };

        public static void CreateFiles(string root, params string[][] multipleTargets)
        {
            foreach (var targets in multipleTargets)
            {    
                foreach (var target in targets)
                {
                    var file = Path.Combine(root, target);
                    CreateFile(file);
                }
            }

            var allFiles = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories);
            Console.WriteLine($"Created files under {root}:");
            foreach (var file in allFiles.Select(f => f.Substring(root.Length)))
            {
                Console.WriteLine($"\t{file}");
            }
        }

        public static string CreateFile(string target)
        {
            var directory = Directory.GetParent(target).FullName;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var contents = Guid.NewGuid().ToString();
            File.WriteAllText(target, contents);

            return contents;
        }
    }
}