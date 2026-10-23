using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace OpenDeviceToolkit.Core.Ai;

/// <summary>
/// Thin wrapper over the Windows Data Protection API (DPAPI), CurrentUser
/// scope. Used so AI provider keys never sit on disk in plaintext. On
/// non-Windows platforms the calls fail; callers must treat that as an
/// empty key store rather than an application failure.
/// </summary>
internal static class Dpapi
{
    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob
    {
        public int Size;
        public IntPtr Data;
    }

    [DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CryptProtectData(ref DataBlob dataIn, string? description, IntPtr entropy, IntPtr reserved, IntPtr prompt, int flags, out DataBlob dataOut);

    [DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CryptUnprotectData(ref DataBlob dataIn, IntPtr description, IntPtr entropy, IntPtr reserved, IntPtr prompt, int flags, out DataBlob dataOut);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LocalAlloc(uint flags, UIntPtr bytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LocalFree(IntPtr memory);

    private const uint LmemZeroInitialize = 0x0008;

    public static byte[] Protect(byte[] plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        if (plaintext.Length == 0)
            throw new ArgumentException("Plaintext must not be empty.", nameof(plaintext));

        var dataIn = AllocateBlob(plaintext);
        try
        {
            if (!CryptProtectData(ref dataIn, "OpenDeviceToolkit.Ai", IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, out var dataOut))
                throw new CryptographicException("DPAPI CryptProtectData failed.");
            try
            {
                return CopyOut(dataOut);
            }
            finally
            {
                LocalFree(dataOut.Data);
            }
        }
        finally
        {
            LocalFree(dataIn.Data);
        }
    }

    public static byte[] Unprotect(byte[] encrypted)
    {
        ArgumentNullException.ThrowIfNull(encrypted);
        if (encrypted.Length == 0)
            throw new ArgumentException("Encrypted data must not be empty.", nameof(encrypted));

        var dataIn = AllocateBlob(encrypted);
        try
        {
            if (!CryptUnprotectData(ref dataIn, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, out var dataOut))
                throw new CryptographicException("DPAPI CryptUnprotectData failed.");
            try
            {
                return CopyOut(dataOut);
            }
            finally
            {
                LocalFree(dataOut.Data);
            }
        }
        finally
        {
            LocalFree(dataIn.Data);
        }
    }

    private static DataBlob AllocateBlob(byte[] data)
    {
        var pointer = LocalAlloc(LmemZeroInitialize, (UIntPtr)data.Length);
        if (pointer == IntPtr.Zero)
            throw new CryptographicException("LocalAlloc failed while preparing DPAPI data.");
        Marshal.Copy(data, 0, pointer, data.Length);
        return new DataBlob { Size = data.Length, Data = pointer };
    }

    private static byte[] CopyOut(DataBlob blob)
    {
        var result = new byte[blob.Size];
        Marshal.Copy(blob.Data, result, 0, blob.Size);
        return result;
    }
}
