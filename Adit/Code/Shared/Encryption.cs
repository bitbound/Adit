using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Adit.Code.Shared
{
    public class Encryption
    {
        public byte[] Key { get; set; }

        private byte[] PartialDecryptionBuffer { get; set; } = new byte[0];

        public async Task<byte[]> EncryptBytes(byte[] bytes)
        {
            var iv = new byte[16];
            RNGCryptoServiceProvider.Create().GetBytes(iv);
            using (var aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = iv;
                using (var encryptor = aes.CreateEncryptor(Key, iv))
                {
                    using (var ms = new MemoryStream())
                    {
                        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            await cs.WriteAsync(bytes, 0, bytes.Length);
                        }
                        return iv.Concat(ms.ToArray()).ToArray();
                    }
                }
            }
        }
        public async Task<byte[]> DecryptBytes(byte[] bytes, bool appendToPartial)
        {
            try
            {
                var iv = bytes.Take(16).ToArray();
                using (var aes = Aes.Create())
                {
                    aes.Key = Key;
                    aes.IV = iv;
                    int bytesRead;
                    byte[] buffer;
                    using (var decryptor = aes.CreateDecryptor(Key, iv))
                    {
                        using (var ms = new MemoryStream(bytes.Skip(16).ToArray()))
                        {
                            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                            {
                                buffer = new byte[bytes.Skip(16).Count()];
                                bytesRead = await cs.ReadAsync(buffer, 0, buffer.Length);
                            }
                            PartialDecryptionBuffer = new byte[0];
                            return buffer.Take(bytesRead).ToArray();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
                if (appendToPartial)
                {
                    PartialDecryptionBuffer = PartialDecryptionBuffer.Concat(bytes).ToArray();
                }
                return null;
            }
        }
    }
}
