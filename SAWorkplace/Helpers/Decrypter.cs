using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using LoggerService;

namespace SAWorkplace.Helpers
{
    public class Decrypter
    {
        private static ILoggerManager _logger;
        public Decrypter(ILoggerManager logger)
        {
            _logger = logger;
        }
        /// <summary>
        /// Decrypt method provided by Jim Girard as part of the new encryption on usernames and passwords requirement.
        /// </summary>
        /// <param name="key">The cryptography key needed to decrypt the encrypted keyName stored in the config.</param>
        /// <param name="cipherString">The name of the key to read from appSettings.json.</param>
        /// <returns>The decrypted value of cipherString.</returns>
        public string Decrypt(string key, string cipherString)
        {
            try
            {
                AesCryptoServiceProvider aes = new AesCryptoServiceProvider
                {
                    BlockSize = 128,
                    KeySize = 256,
                    IV = Encoding.UTF8.GetBytes(@"!QAZ2WSX#EDC4RFV"),
                    Key = Encoding.UTF8.GetBytes(key),
                    Mode = CipherMode.CBC,
                    Padding = PaddingMode.PKCS7
                };

                // Convert Base64 strings to byte array
                byte[] src = Convert.FromBase64String(cipherString);

                // decryption
                using ICryptoTransform decrypt = aes.CreateDecryptor();
                byte[] dest = decrypt.TransformFinalBlock(src, 0, src.Length);
                aes.Dispose();
                return Encoding.Unicode.GetString(dest);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Exception Message. {e.Message} {e.StackTrace};");
                return "";
            }
        }
    }
}
