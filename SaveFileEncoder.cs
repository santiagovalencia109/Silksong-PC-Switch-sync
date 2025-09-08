using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SwitchFileSync
{
    public static class SaveFileEncoder
    {
        // 🔑 Clave fija de 32 caracteres (AES-256 ECB)
        private static readonly byte[] aesKey = Encoding.UTF8.GetBytes("UKu52ePUBwetZ9wNX88o54dnfKRu0T1l");

        // Cabecera fija (igual que en functions.js)
        private static readonly byte[] cSharpHeader = new byte[]
        {
            0, 1, 0, 0, 0, 255, 255, 255, 255, 1, 0, 0, 0, 0, 0, 0, 0, 6, 1, 0, 0, 0
        };

        /// <summary>
        /// Decodifica un archivo .dat y devuelve el contenido como string JSON.
        /// </summary>
        public static string DecodeDatFile(string path)
        {
            byte[] fileBytes = File.ReadAllBytes(path);

            // 1. Remove header
            byte[] withoutHeader = RemoveHeader(fileBytes);

            // 2. Base64 decode
            byte[] base64Decoded = CustomBase64.Decode(withoutHeader);

            // 3. AES decrypt + quitar padding
            byte[] decrypted = AESDecrypt(base64Decoded);

            // 4. Bytes -> string
            return Encoding.UTF8.GetString(decrypted);
        }

        /// <summary>
        /// Codifica un string JSON en un archivo .dat.
        /// </summary>
        public static void EncodeDatFile(string json, string outputPath)
        {
            // 1. String -> bytes
            byte[] plainBytes = Encoding.UTF8.GetBytes(json);

            // 2. AES encrypt + padding
            byte[] encrypted = AESEncrypt(plainBytes);

            // 3. Base64 encode
            byte[] base64Encoded = CustomBase64.Encode(encrypted);

            // 4. Add header
            byte[] finalBytes = AddHeader(base64Encoded);

            // 5. Guardar archivo
            File.WriteAllBytes(outputPath, finalBytes);
        }

        // ========== AES ==========

        private static byte[] AESDecrypt(byte[] data)
        {
            using (var aes = Aes.Create())
            {
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;
                aes.Key = aesKey;

                using (var decryptor = aes.CreateDecryptor())
                {
                    byte[] decrypted = decryptor.TransformFinalBlock(data, 0, data.Length);

                    // quitar PKCS7 padding
                    int pad = decrypted[decrypted.Length - 1];
                    int len = decrypted.Length - pad;
                    byte[] unpadded = new byte[len];
                    Array.Copy(decrypted, 0, unpadded, 0, len);
                    return unpadded;
                }
            }
        }

        private static byte[] AESEncrypt(byte[] data)
        {
            // aplicar PKCS7 padding manual
            int pad = 16 - (data.Length % 16);
            byte[] padded = new byte[data.Length + pad];
            Array.Copy(data, padded, data.Length);
            for (int i = data.Length; i < padded.Length; i++)
                padded[i] = (byte)pad;

            using (var aes = Aes.Create())
            {
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;
                aes.Key = aesKey;

                using (var encryptor = aes.CreateEncryptor())
                {
                    return encryptor.TransformFinalBlock(padded, 0, padded.Length);
                }
            }
        }

        // ========== Header ==========

        private static byte[] AddHeader(byte[] data)
        {
            byte[] lengthData = GenerateLengthPrefixedString(data.Length);
            byte[] finalBytes = new byte[cSharpHeader.Length + lengthData.Length + data.Length + 1];

            Buffer.BlockCopy(cSharpHeader, 0, finalBytes, 0, cSharpHeader.Length);
            Buffer.BlockCopy(lengthData, 0, finalBytes, cSharpHeader.Length, lengthData.Length);
            Buffer.BlockCopy(data, 0, finalBytes, cSharpHeader.Length + lengthData.Length, data.Length);

            finalBytes[finalBytes.Length - 1] = 11; // byte fijo

            return finalBytes;
        }

        private static byte[] RemoveHeader(byte[] data)
        {
            // quitar cabecera fija y byte final 11
            byte[] trimmed = new byte[data.Length - cSharpHeader.Length - 1];
            Buffer.BlockCopy(data, cSharpHeader.Length, trimmed, 0, trimmed.Length);

            // quitar LengthPrefixedString
            int lengthCount = 0;
            for (int i = 0; i < 5; i++)
            {
                lengthCount++;
                if ((trimmed[i] & 0x80) == 0)
                {
                    break;
                }
            }

            byte[] result = new byte[trimmed.Length - lengthCount];
            Buffer.BlockCopy(trimmed, lengthCount, result, 0, result.Length);

            return result;
        }

        private static byte[] GenerateLengthPrefixedString(int length)
        {
            length = Math.Min(0x7FFFFFFF, length);
            using (var ms = new MemoryStream())
            {
                while (true)
                {
                    if (length >> 7 != 0)
                    {
                        ms.WriteByte((byte)((length & 0x7F) | 0x80));
                        length >>= 7;
                    }
                    else
                    {
                        ms.WriteByte((byte)(length & 0x7F));
                        length >>= 7;
                        break;
                    }
                }
                if (length != 0)
                {
                    ms.WriteByte((byte)length);
                }
                return ms.ToArray();
            }
        }
    }

    // ==========================
    // Base64 personalizado (igual a base64.js)
    // ==========================
    public static class CustomBase64
    {
        private static readonly char[] BASE64_ARRAY = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=".ToCharArray();

        private static readonly byte[] ENCODE_TABLE = new byte[65];
        private static readonly byte[] DECODE_TABLE = new byte[128];

        static CustomBase64()
        {
            for (int i = 0; i < BASE64_ARRAY.Length; i++)
            {
                ENCODE_TABLE[i] = (byte)BASE64_ARRAY[i];
            }

            for (int i = 0; i < BASE64_ARRAY.Length; i++)
            {
                if (BASE64_ARRAY[i] < 128)
                {
                    DECODE_TABLE[BASE64_ARRAY[i]] = (byte)i;
                }
            }
        }

        public static byte[] Encode(byte[] buffer)
        {
            using (var ms = new MemoryStream())
            {
                int i = 0;
                for (; i + 2 < buffer.Length; i += 3)
                {
                    ms.WriteByte(ENCODE_TABLE[buffer[i] >> 2]);
                    ms.WriteByte(ENCODE_TABLE[((buffer[i] & 0x03) << 4) | (buffer[i + 1] >> 4)]);
                    ms.WriteByte(ENCODE_TABLE[((buffer[i + 1] & 0x0F) << 2) | (buffer[i + 2] >> 6)]);
                    ms.WriteByte(ENCODE_TABLE[buffer[i + 2] & 0x3F]);
                }

                if (i < buffer.Length)
                {
                    ms.WriteByte(ENCODE_TABLE[buffer[i] >> 2]);
                    if (i + 1 < buffer.Length)
                    {
                        ms.WriteByte(ENCODE_TABLE[((buffer[i] & 0x03) << 4) | (buffer[i + 1] >> 4)]);
                        ms.WriteByte(ENCODE_TABLE[(buffer[i + 1] & 0x0F) << 2]);
                        ms.WriteByte((byte)'=');
                    }
                    else
                    {
                        ms.WriteByte(ENCODE_TABLE[(buffer[i] & 0x03) << 4]);
                        ms.WriteByte((byte)'=');
                        ms.WriteByte((byte)'=');
                    }
                }

                return ms.ToArray();
            }
        }

        public static byte[] Decode(byte[] buffer)
        {
            int padding = 0;
            if (buffer[buffer.Length - 1] == (byte)'=') padding++;
            if (buffer[buffer.Length - 2] == (byte)'=') padding++;

            int length = (buffer.Length * 3) / 4 - padding;
            byte[] output = new byte[length];

            int bufferIndex = 0, outputIndex = 0;
            while (bufferIndex < buffer.Length)
            {
                int b1 = DECODE_TABLE[buffer[bufferIndex++]];
                int b2 = DECODE_TABLE[buffer[bufferIndex++]];
                int b3 = DECODE_TABLE[buffer[bufferIndex++]];
                int b4 = DECODE_TABLE[buffer[bufferIndex++]];

                output[outputIndex++] = (byte)((b1 << 2) | (b2 >> 4));
                if (outputIndex < length)
                    output[outputIndex++] = (byte)(((b2 & 0x0F) << 4) | (b3 >> 2));
                if (outputIndex < length)
                    output[outputIndex++] = (byte)(((b3 & 0x03) << 6) | b4);
            }

            return output;
        }
    }
}
