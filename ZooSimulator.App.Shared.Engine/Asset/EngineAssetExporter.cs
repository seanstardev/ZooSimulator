using System;
using System.IO;
using System.Reflection;

namespace ZooSimulator.App.Shared.Engine
{
    /// <summary>Exports the bundled offline scene and licences beneath the executable directory.</summary>
    public static class EngineAssetExporter
    {
        /// <summary>Writes owned embedded assets into an Engine child folder and returns its path.</summary>
        /// <remarks>The supplied directory must be writable. No external files are deleted.</remarks>
        public static string Export(string applicationDirectory)
        {
            if (applicationDirectory == null)
                throw new ArgumentNullException(nameof(applicationDirectory));

            if (Path.IsPathRooted(applicationDirectory) == false)
            {
                throw new ArgumentException(
                    "An absolute application directory is required.",
                    nameof(applicationDirectory));
            }

            string directory = Path.GetFullPath(
                Path.Combine(
                    applicationDirectory,
                    "Engine"));

            string prefix = directory + Path.DirectorySeparatorChar;

            var assembly = typeof(EngineAssetExporter).GetTypeInfo().Assembly;

            Directory.CreateDirectory(directory);

            foreach (string name in assembly.GetManifestResourceNames())
            {
                if (name.StartsWith("EngineAsset.", StringComparison.Ordinal) == false)
                    continue;

                string relative = name.Substring("EngineAsset.".Length)
                    .Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);

                string target = Path.GetFullPath(Path.Combine(directory, relative));

                if (target.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == false)
                {
                    throw new InvalidOperationException(
                        "An embedded asset escaped the Engine directory.");
                }

                Directory.CreateDirectory(Path.GetDirectoryName(target)!);

                using (var source = assembly.GetManifestResourceStream(name)
                    ?? throw new InvalidOperationException(
                        "An embedded engine asset was not found."))
                using (var destination = File.Create(target))
                {
                    source.CopyTo(destination);
                }
            }

            if (File.Exists(Path.Combine(directory, "index.html")) == false)
            {
                throw new InvalidOperationException(
                    "The engine entry document is missing.");
            }

            return directory;
        }
    }
}
