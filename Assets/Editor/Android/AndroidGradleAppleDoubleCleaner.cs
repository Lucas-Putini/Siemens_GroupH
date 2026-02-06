// Cleans macOS AppleDouble files (._*) that can break Android CMake builds.
//
// On non-APFS volumes (exFAT/NTFS/network shares), macOS may create "._<name>" sidecar files
// to store extended attributes. Unity's generated GameActivity CMakeLists.txt uses a glob that
// picks up all *.cpp, so a file like "._UGAConfiguration.cpp" gets compiled and fails due to
// embedded null bytes.
//
// This post-generate hook removes those files from the generated Gradle project before Gradle runs.
#if UNITY_ANDROID
using System;
using System.IO;
using UnityEditor.Android;
using UnityEngine;

public sealed class AndroidGradleAppleDoubleCleaner : IPostGenerateGradleAndroidProject
{
    // Run as early as possible.
    public int callbackOrder => int.MinValue;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            return;

        try
        {
            var deleted = 0;

            deleted += DeleteAllMatchingFiles(path, "._*");
            deleted += DeleteAllMatchingFiles(path, ".DS_Store");

            if (deleted > 0)
                Debug.Log($"[AndroidGradleAppleDoubleCleaner] Deleted {deleted} AppleDouble/.DS_Store files under: {path}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AndroidGradleAppleDoubleCleaner] Cleanup failed under '{path}'.\n{e}");
        }
    }

    static int DeleteAllMatchingFiles(string root, string searchPattern)
    {
        var count = 0;

        foreach (var file in Directory.EnumerateFiles(root, searchPattern, SearchOption.AllDirectories))
        {
            try
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
                count++;
            }
            catch
            {
                // Best-effort cleanup; don't block the build for a cleanup failure.
            }
        }

        return count;
    }
}
#endif

