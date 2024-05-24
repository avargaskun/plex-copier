namespace tst
{
    public static class TestFiles
    {
        public static readonly string[] SingleSeries =
        [
            Path.Combine("Single_Series", "[TEST] Single - 01.mkv"),
            Path.Combine("Single_Series", "[TEST] Single - 02.mkv"),
            Path.Combine("Single_Series", "[TEST] Single - 03.mkv"),
        ];

        public static readonly string[] DoubleSeries =
        [
            Path.Combine("Double_Series", "Season-01", "[TEST] Double S1 - 01.mkv"),
            Path.Combine("Double_Series", "Season-01", "[TEST] Double S1 - 02.mkv"),
            Path.Combine("Double_Series", "Season-01", "[TEST] Double S1 - 03.mkv"),

            Path.Combine("Double_Series", "Season-02", "[TEST] Double S2 - 01.mkv"),
            Path.Combine("Double_Series", "Season-02", "[TEST] Double S2 - 02.mkv"),
            Path.Combine("Double_Series", "Season-02", "[TEST] Double S2 - 03.mkv"),
        ];

        public static readonly string[] LongSeries =
        [
            Path.Combine("Long_Series", "[TEST] Long - 01.mp4"),
            Path.Combine("Long_Series", "[TEST] Long - 02.mp4"),
            Path.Combine("Long_Series", "[TEST] Long - 03.mp4"),
            Path.Combine("Long_Series", "[TEST] Long - 04.mp4"),
            Path.Combine("Long_Series", "[TEST] Long - 05.mp4"),
            Path.Combine("Long_Series", "[TEST] Long - 06.mp4"),
        ];

        public static readonly string[] SeriesWithSpecials =
        [
            Path.Combine("Series_With_Specials", "Season-00", "[TEST] Specials - 01.mkv"),
            Path.Combine("Series_With_Specials", "Season-00", "[TEST] Specials - 02.mkv"),
            Path.Combine("Series_With_Specials", "Season-00", "[TEST] Specials - 03.mkv"),
            Path.Combine("Series_With_Specials", "Season-00", "[TEST] Specials - 04.mkv"),
            Path.Combine("Series_With_Specials", "Season-00", "[TEST] Specials - Movie.mkv"),

            Path.Combine("Series_With_Specials", "Season-01", "[TEST] Single S1 - 01.mkv"),
            Path.Combine("Series_With_Specials", "Season-01", "[TEST] Single S1 - 02.mkv"),
            Path.Combine("Series_With_Specials", "Season-01", "[TEST] Single S1 - 03.mkv"),
        ];

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
            var directory = Directory.GetParent(target)!.FullName;
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