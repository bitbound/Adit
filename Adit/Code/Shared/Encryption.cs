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

        public byte[] EncryptBytes(byte[] bytes)
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
                            cs.Write(bytes, 0, bytes.Length);
                        }
                        return iv.Concat(ms.ToArray()).ToArray();
                    }
                }
            }
        }
        public byte[] DecryptBytes(byte[] bytes)
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
                                bytesRead = cs.Read(buffer, 0, buffer.Length);
                            }
                            return buffer.Take(bytesRead).ToArray();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
                return null;
            }
        }

        public static byte[] GetServerKey()
        {
            var keyPath = Path.Combine(Utilities.DataFolder, "ServerKey");
            return GetKey(keyPath);
        }
        public static byte[] GetClientKey()
        {
            var keyPath = Path.Combine(Utilities.DataFolder, "ClientKey");
            return GetKey(keyPath);
        }
        private static byte[] GetKey(string keyPath)
        {
            if (File.Exists(keyPath))
            {
                try
                {
                    return File.ReadAllBytes(keyPath);
                }
                catch
                {
                    System.Windows.MessageBox.Show("Unable to read encryption key file.  You may not have access to it.  The file can only be read by the account that created it.", "Read Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return null;
                }
            }
            else
            {
                System.Windows.MessageBox.Show("No encryption key was found.", "Key Not Found", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return null;
            }
        }
        public static byte[] CreateNewKey()
        {
            var keyPath = Path.Combine(Utilities.DataFolder, "ServerKey");
            using (var rng = RandomNumberGenerator.Create())
            {
                var key = new byte[32];
                rng.GetBytes(key);
                var id = Guid.NewGuid().ToString();
                File.WriteAllBytes(Path.Combine(keyPath), key);
                File.Encrypt(keyPath);
                return key;
            }
        }
    }
}
