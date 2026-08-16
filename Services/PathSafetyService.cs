using System;
using System.IO;

namespace MSFSCacheManager.Services
{
    public static class PathSafetyService
    {
        public static bool PathsOverlap(
            string firstPath,
            string secondPath)
        {
            return IsSameOrWithin(firstPath, secondPath) ||
                   IsSameOrWithin(secondPath, firstPath);
        }

        public static bool IsSameOrWithin(
            string candidatePath,
            string parentPath)
        {
            string candidate = NormalizePath(candidatePath);
            string parent = NormalizePath(parentPath);

            return string.Equals(
                       candidate,
                       parent,
                       StringComparison.OrdinalIgnoreCase) ||
                   IsWithin(candidate, parent);
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(
                    "A path cannot be empty.",
                    nameof(path));
            }

            return Path.TrimEndingDirectorySeparator(
                Path.GetFullPath(path.Trim()));
        }

        private static bool IsWithin(
            string candidatePath,
            string parentPath)
        {
            string parentWithSeparator =
                Path.EndsInDirectorySeparator(parentPath)
                    ? parentPath
                    : parentPath + Path.DirectorySeparatorChar;

            return candidatePath.StartsWith(
                parentWithSeparator,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
