using System;
using System.IO;
using System.Security.Cryptography;

namespace FEx.Legacy.IO;

/// <summary>
/// https://www.codeproject.com/Articles/22736/Securely-Delete-a-File-using-NET
/// </summary>
public class Wipe
{
    public event PassInfoEventHandler PassInfoEvent;

    public event SectorInfoEventHandler SectorInfoEvent;

    public event WipeDoneEventHandler WipeDoneEvent;

    public event WipeErrorEventHandler WipeErrorEvent;

    public static Wipe Instance { get; } = new();

    private Wipe()
    {
    }

    /// <summary>
    /// Deletes a file in a secure way by overwriting it with
    /// random garbage data n times.
    /// </summary>
    /// <param name="filename">Full path of the file to be deleted</param>
    /// <param name="timesToWrite">Specifies the number of times the file should be overwritten</param>
    public void WipeFile(string filename, int timesToWrite)
    {
        try
        {
            if (File.Exists(filename))
            {
                // Set the files attributes to normal in case it's read-only.
                File.SetAttributes(filename, FileAttributes.Normal);

                // Calculate the total number of sectors in the file.
                var sectors = Math.Ceiling(new FileInfo(filename).Length / 512.0);

                // Create a dummy-buffer the size of a sector.
#if NETSTANDARD
                var dummyBuffer = new byte[512];
                using var rng = new RNGCryptoServiceProvider();
#else
                var dummyBuffer = new Span<byte>(new byte[512]);
#endif
                using var inputStream = new FileStream(filename, FileMode.Open);

                for (var currentPass = 0; currentPass < timesToWrite; currentPass++)
                {
                    UpdatePassInfo(currentPass + 1, timesToWrite);

                    // Go to the beginning of the stream
                    inputStream.Position = 0;

                    // Loop all sectors
                    for (var sectorsWritten = 0; sectorsWritten < sectors; sectorsWritten++)
                    {
                        UpdateSectorInfo(sectorsWritten + 1, (int)sectors);

                        // Fill the dummy-buffer with random data
#if NETSTANDARD
                        rng.GetBytes(dummyBuffer);
#else
                        RandomNumberGenerator.GetBytes(512).CopyTo(dummyBuffer);
#endif
                        // Write it to the stream
#if NETSTANDARD
                        inputStream.Write(dummyBuffer, 0, dummyBuffer.Length);
#else
                        inputStream.Write(dummyBuffer.ToArray(), 0, dummyBuffer.Length);
#endif
                    }
                }

                // Truncate the file to 0 bytes.
                // This will hide the original file-length if you try to recover the file.
                inputStream.SetLength(0);
                // Close the stream.
                inputStream.Close();

                // As an extra precaution I change the dates of the file so the
                // original dates are hidden if you try to recover the file.
                var dt = new DateTime(2037, 1, 1, 0, 0, 0);
                File.SetCreationTime(filename, dt);
                File.SetLastAccessTime(filename, dt);
                File.SetLastWriteTime(filename, dt);

                File.SetCreationTimeUtc(filename, dt);
                File.SetLastAccessTimeUtc(filename, dt);
                File.SetLastWriteTimeUtc(filename, dt);

                // Finally, delete the file
                File.Delete(filename);

                WipeDone();
            }
        }
        catch (Exception e)
        {
            WipeError(e);
        }
    }

    private void UpdatePassInfo(int currentPass, int totalPasses) =>
        PassInfoEvent?.Invoke(new(currentPass, totalPasses));

    private void UpdateSectorInfo(int currentSector, int totalSectors) =>
        SectorInfoEvent?.Invoke(new(currentSector, totalSectors));

    private void WipeDone() => WipeDoneEvent?.Invoke(new());

    private void WipeError(Exception e) => WipeErrorEvent?.Invoke(new(e));
}

public delegate void PassInfoEventHandler(PassInfoEventArgs e);

public class PassInfoEventArgs : EventArgs
{
    /// <summary> Get the current pass </summary>
    public int CurrentPass { get; }

    /// <summary> Get the total number of passes to be run </summary>
    public int TotalPasses { get; }

    public PassInfoEventArgs(int currentPass, int totalPasses)
    {
        CurrentPass = currentPass;
        TotalPasses = totalPasses;
    }
}

public delegate void SectorInfoEventHandler(SectorInfoEventArgs e);

public class SectorInfoEventArgs : EventArgs
{
    /// <summary> Get the current sector </summary>
    public int CurrentSector { get; }

    /// <summary> Get the total number of sectors to be run </summary>
    public int TotalSectors { get; }

    public SectorInfoEventArgs(int currentSector, int totalSectors)
    {
        CurrentSector = currentSector;
        TotalSectors = totalSectors;
    }
}

public delegate void WipeDoneEventHandler(WipeDoneEventArgs e);

public class WipeDoneEventArgs : EventArgs
{
}

public delegate void WipeErrorEventHandler(WipeErrorEventArgs e);

public class WipeErrorEventArgs : EventArgs
{
    public Exception WipeError { get; }

    public WipeErrorEventArgs(Exception error)
    {
        WipeError = error;
    }
}