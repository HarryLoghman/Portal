using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary
{
    public static class Encrypt
    {
        public static string GetSha256Hash(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            SHA256Managed hashstring = new SHA256Managed();

            byte[] hash = hashstring.ComputeHash(bytes);
            string hashString = string.Empty;
            foreach (byte x in hash)
            {
                hashString += String.Format("{0:x2}", x);
            }
            return hashString;
        }

        public static string GetHMACSHA256Hash(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            System.Text.ASCIIEncoding encoding = new ASCIIEncoding();
            byte[] keyBytes = encoding.GetBytes("dehnad");
            HMACSHA256 hashstring = new HMACSHA256(keyBytes);

            byte[] hash = hashstring.ComputeHash(bytes);
            string hashString = string.Empty;
            foreach (byte x in hash)
            {
                hashString += String.Format("{0:x2}", x);
            }
            return hashString;
        }

        // This size of the IV (in bytes) must = (keysize / 8).  Default keysize is 256, so the IV must be
        // 32 bytes long.  Using a 16 character string here gives us 32 bytes when converted to a byte array.

        private const string fld_initVector = "dehnadportalcryp";
        // This constant is used to determine the keysize of the encryption algorithm
        private const int fld_keysize = 256;
        private const string fldـpassPhrase = "localcallcrypt";
        //Encrypt
        public static string EncryptString(string plainText)
        {
            return EncryptString(plainText, fld_keysize, fld_initVector, fldـpassPhrase);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="plainText"></param>
        /// <param name="keySize"></param>
        /// <param name="IV">keysize/8/2</param>
        /// <param name="keyVector"></param>
        /// <returns></returns>
        public static string EncryptString(string plainText, int keySize, string IV, string keyVector)
        {
            if (keySize != 128 && keySize != 192 && keySize != 256)
                throw new ArgumentException("Wrong key size (allowed sizes are 128,192 and 256)", "keysize");
            if (string.IsNullOrEmpty(IV))
                throw new ArgumentException("IV is not specified", "IV");
            if (string.IsNullOrEmpty(keyVector))
                throw new ArgumentException("keyVector is not specified", "keyVector");
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentException("plainText is not specified", "plainText");

            if (Encoding.UTF8.GetByteCount(IV) != keySize / 8 / 2)
            {
                throw new Exception("IV byte count is n0t equal with keysize/8/2");
            }

            if (plainText.Length > 1024)
            {
                throw new ArgumentException("long parameter", "plainText");
            }
            if (IV.Length > 1024)
            {
                throw new ArgumentException("long parameter", "IV");
            }
            if (keyVector.Length > 1024)
            {
                throw new ArgumentException("long parameter", "keyvector");
            }
            byte[] initVectorBytes = Encoding.UTF8.GetBytes(IV);
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            PasswordDeriveBytes password = new PasswordDeriveBytes(keyVector, null);
            byte[] keyBytes = password.GetBytes(keySize / 8);
            RijndaelManaged symmetricKey = new RijndaelManaged();
            symmetricKey.Mode = CipherMode.CBC;
            ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, initVectorBytes);
            MemoryStream memoryStream = new MemoryStream();
            CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
            cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
            cryptoStream.FlushFinalBlock();
            byte[] cipherTextBytes = memoryStream.ToArray();
            memoryStream.Close();
            cryptoStream.Close();
            return Convert.ToBase64String(cipherTextBytes);
        }
        //Decrypt
        public static string DecryptString(string cipherText)
        {
            return DecryptString(cipherText, fld_keysize, fld_initVector, fldـpassPhrase);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cipherText"></param>
        /// <param name="keySize"></param>
        /// <param name="IV">keysize/8/2</param>
        /// <param name="keyVector"></param>
        /// <returns></returns>
        public static string DecryptString(string cipherText, int keySize, string IV, string keyVector)
        {
            if (keySize != 128 && keySize != 192 && keySize != 256)
                throw new ArgumentException("Wrong key size (allowed sizes are 128,192 and 256)", "keysize");
            if (string.IsNullOrEmpty(IV))
                throw new ArgumentException("IV is not specified", "IV");
            if (string.IsNullOrEmpty(keyVector))
                throw new ArgumentException("keyVector is not specified", "keyVector");
            if (string.IsNullOrEmpty(cipherText))
                throw new ArgumentException("cipherText is not specified", "cipherText");

            if (Encoding.UTF8.GetByteCount(IV) != keySize / 8 / 2)
            {
                throw new Exception("IV byte count is not equal with keysize/8/2");
            }
            
            if (IV.Length > 1024)
            {
                throw new ArgumentException("long parameter", "IV");
            }
            if (keyVector.Length > 1024)
            {
                throw new ArgumentException("long parameter", "keyvector");
            }

            byte[] IVBytes = Encoding.UTF8.GetBytes(IV);
            byte[] cipherTextBytes = Convert.FromBase64String(cipherText);
            PasswordDeriveBytes password = new PasswordDeriveBytes(keyVector, null);
            byte[] keyBytes = password.GetBytes(fld_keysize / 8);
            RijndaelManaged symmetricKey = new RijndaelManaged();
            symmetricKey.Mode = CipherMode.CBC;
            ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, IVBytes);
            MemoryStream memoryStream = new MemoryStream(cipherTextBytes);
            CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
            byte[] plainTextBytes = new byte[cipherTextBytes.Length];
            int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
            memoryStream.Close();
            cryptoStream.Close();
            return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
        }

        public static bool fnc_detectApp(string appName, string plainText, string cipherText
            , out string errorType, out string errorDescription)
        {
            errorType = "";
            errorDescription = "";

            if (string.IsNullOrEmpty(cipherText))
            {
                errorType = "cipherText is not specified";
                return false;
            }

            try
            {
                string encryptedText = fnc_enryptAppParameter(appName, plainText, out errorType, out errorDescription);
                if (!string.IsNullOrEmpty(errorType)) return false;

                if (encryptedText != cipherText)
                {
                    errorType = "Wrong detection parameter is passed";
                    errorDescription = "AppName = " + appName + " plainText = " + plainText
                        + " cipherText = " + cipherText;
                    return false;
                }
                else return true;

            }
            catch (Exception ex)
            {
                errorType = "Exception has been occured!!! Contact Administrator";
                errorDescription = "AppName = " + appName + " exception : " + ex.Message;
                return false;
            }
            return false;
        }

        public static string fnc_enryptAppParameter(string appName, string plainText
            , out string errorType, out string errorDescription)
        {
            errorType = "";
            errorDescription = "";
            if (string.IsNullOrEmpty(appName))
            {
                errorType = "AppName is not specified";
                return "";
            }
            if (string.IsNullOrEmpty(plainText))
            {
                errorType = "PlaintText is not specified";
                return "";
            }

            try
            {
                using (var portal = new SharedLibrary.Models.PortalEntities())
                {
                    var app = portal.Apps.Where(o => o.appName == appName).FirstOrDefault();
                    if (app == null)
                    {
                        errorType = "AppName is not defined";
                        errorDescription = "AppName = " + appName;
                        return "";
                    }
                    if (!app.state.HasValue || app.state.Value == 0)
                    {
                        errorType = "AppName is disabled";
                        errorDescription = "AppName = " + appName;
                        return "";
                    }
                    if (app.keySize / 8 / 2 != System.Text.Encoding.UTF8.GetByteCount(app.IV))
                    {
                        errorType = "IV byte length is not equal with keysize/16";
                        errorDescription = "AppName = " + appName;
                        return "";
                    }

                    try
                    {
                        string encryptedText = EncryptString(plainText, app.keySize, app.IV, app.keyVector);
                        return encryptedText;
                    }
                    catch (Exception exInner)
                    {
                        errorType = "Exception has been occured!!! Contact Administrator";
                        errorDescription = "AppName = " + appName + " exception : " + exInner.Message;
                        return "";

                    }


                }
            }
            catch (Exception ex)
            {
                errorType = "Exception has been occured!!! Contact Administrator";
                errorDescription = "AppName = " + appName + " exception : " + ex.Message;
                return "";
            }
            return "";
        }


    }

}
