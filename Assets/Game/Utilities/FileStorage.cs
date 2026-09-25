using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

public static class FileStorage
{
    // The caller creates the destination directory and serializes concurrent writes.
    public static UniTask WriteAtomicAsync(string path, string content, CancellationToken cancellationToken = default) =>
        WriteAtomicAsync(path, temporaryPath => File.WriteAllTextAsync(temporaryPath, content, cancellationToken), cancellationToken);

    public static UniTask WriteAtomicAsync(string path, byte[] content, CancellationToken cancellationToken = default) =>
        WriteAtomicAsync(path, temporaryPath => File.WriteAllBytesAsync(temporaryPath, content, cancellationToken), cancellationToken);

    private static async UniTask WriteAtomicAsync(string path, Func<string, Task> write, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";

        try
        {
            await write(temporaryPath);
            cancellationToken.ThrowIfCancellationRequested();

            if (File.Exists(path))
                File.Replace(temporaryPath, path, null);
            else
                File.Move(temporaryPath, path);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    // Move a file or a whole save folder into the archive.
    public static string MoveToArchive(string path, string archiveDirectory)
    {
        Directory.CreateDirectory(archiveDirectory);
        var archivedPath = Path.Combine(
            archiveDirectory,
            Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N"));

        if (Directory.Exists(path))
            Directory.Move(path, archivedPath);
        else
            File.Move(path, archivedPath);
        return archivedPath;
    }
}
