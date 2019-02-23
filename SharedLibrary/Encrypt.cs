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
        public static string EncryptString_RijndaelManaged(string plainText)
        {
            return EncryptString_RijndaelManaged(plainText, fld_keysize, fld_initVector, fldـpassPhrase);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="plainText"></param>
        /// <param name="keySize"></param>
        /// <param name="IV">keysize/8/2</param>
        /// <param name="keyVector"></param>
        /// <returns></returns>
        public static string EncryptString_RijndaelManaged(string plainText, int keySize, string IV, string keyVector)
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
                throw new Exception("IV byte count is not equal with keysize/8/2");
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
        public static string DecryptString_RijndaelManaged(string cipherText)
        {
            return DecryptString_RijndaelManaged(cipherText, fld_keysize, fld_initVector, fldـpassPhrase);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cipherText"></param>
        /// <param name="keySize"></param>
        /// <param name="IV">keysize/8/2</param>
        /// <param name="keyVector"></param>
        /// <returns></returns>
        public static string DecryptString_RijndaelManaged(string cipherText, int keySize, string IV, string keyVector)
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
            byte[] keyBytes = password.GetBytes(keySize / 8);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="plainText"></param>
        /// <param name="keySize">128,192,256</param>
        /// <param name="IV">with length 16</param>
        /// <param name="keyVector">with length keysize/8</param>
        /// <returns></returns>
        public static string EncryptString_AES(string plainText, int keySize, string IV, string keyVector)
        {
            byte[] btsIV = Encoding.ASCII.GetBytes(IV);
            byte[] btsKey = Encoding.ASCII.GetBytes(keyVector);
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (keySize != 128 && keySize != 192 && keySize != 256)
                throw new ArgumentException("keySize should be 128, 192 or 256");
            if (btsKey == null || btsKey.Length <= 0)
                throw new ArgumentNullException("Key");
            if (btsIV == null || btsIV.Length <= 0)
                throw new ArgumentNullException("IV");
            if (btsIV.Length != 128 / 8)
                throw new ArgumentException("length of IV should be 16");
            if (btsKey.Length != keySize / 8)
                throw new ArgumentException("length of keyVector should be " + keySize / 8);

            byte[] encrypted;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {

                aesAlg.KeySize = keySize;
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Key = btsKey;
                aesAlg.IV = btsIV;

                // Create an encryptor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }


            // Return the encrypted bytes from the memory stream.
            return Convert.ToBase64String(encrypted);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cipherText"></param>
        /// <param name="keySize">128,192,256</param>
        /// <param name="IV">with length 16</param>
        /// <param name="keyVector">with length keysize/8</param>
        /// <returns></returns>
        public static string DecryptString_AES(string cipherText, int keySize, string IV, string keyVector)
        {
            byte[] btsCipher;
            byte[] btsKey;
            byte[] btsIV;

            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (keySize != 128 && keySize != 192 && keySize != 256)
                throw new ArgumentException("keySize should be 128, 192 or 256");

            btsCipher = Convert.FromBase64String(cipherText);
            btsKey = Encoding.ASCII.GetBytes(keyVector);
            btsIV = Encoding.ASCII.GetBytes(IV);
            if (btsKey == null || btsKey.Length <= 0)
                throw new ArgumentNullException("Key");
            if (btsIV == null || btsIV.Length <= 0)
                throw new ArgumentNullException("IV");
            if (btsIV.Length != 128 / 8)
                throw new ArgumentException("length of IV should be 16");
            if (btsKey.Length != keySize / 8)
                throw new ArgumentException("length of keyVector should be " + keySize / 8);

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = btsKey;
                aesAlg.IV = btsIV;

                // Create a decryptor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(btsCipher))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }

            }

            return plaintext;

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="serviceCode"></param>
        /// <param name="content"></param>
        /// <param name="plainText"></param>
        /// <param name="cipherText"></param>
        /// <param name="checkContent">for SubscriberActivationState this parameter is set to false. since there is no need to check content </param>
        /// <param name="errorType"></param>
        /// <param name="errorDescription"></param>
        /// <returns></returns>
        public static bool fnc_detectApp(string appName, string serviceCode, string content, string plainText, string cipherText
            , bool checkContent, out string errorType, out string errorDescription)
        {
            errorType = "";
            errorDescription = "";
            if (string.IsNullOrEmpty(appName))
            {
                errorType = "AppName is not specified";
                return false;
            }
            if (string.IsNullOrEmpty(cipherText))
            {
                errorType = "cipherText is not specified";
                return false;
            }
            if (string.IsNullOrEmpty(serviceCode))
            {
                errorType = "serviceCode is not specified";
                errorDescription = "AppName = " + appName;
                return false;
            }
            if (checkContent && string.IsNullOrEmpty(content))
            {
                errorType = "content is not specified";
                errorDescription = "AppName = " + appName + " serviceCode = " + serviceCode;
                return false;
            }
            try
            {
                using (var portal = new SharedLibrary.Models.PortalEntities())
                {
                    var entryApp = portal.Apps.Where(o => o.appName == appName).FirstOrDefault();
                    if (entryApp == null)
                    {
                        errorType = "AppName is not defined";
                        errorDescription = "AppName = " + appName;
                        return false;
                    }
                    if (!entryApp.state.HasValue || entryApp.state.Value == 0)
                    {
                        errorType = "App is disabled";
                        errorDescription = "AppName = " + appName;
                        return false;
                    }
                    //if (app.keySize / 8 / 2 != System.Text.Encoding.UTF8.GetByteCount(app.IV))
                    //{
                    //    errorType = "IV byte length is not equal with keysize/16";
                    //    errorDescription = "AppName = " + appName;
                    //    return "";
                    //}

                    string encryptedText = fnc_enryptAppParameter(entryApp.enryptAlghorithm, appName
                        , entryApp.keySize, entryApp.IV, entryApp.keyVector, plainText, out errorType, out errorDescription);
                    if (!string.IsNullOrEmpty(errorType)) return false;

                    if (encryptedText != cipherText)
                    {
                        errorType = "Wrong detection parameter is passed";
                        errorDescription = "AppName = " + appName + " plainText = " + plainText
                            + " cipherText = " + cipherText;
                        return false;
                    }

                    if (string.IsNullOrEmpty(entryApp.allowedServices))
                    {
                        errorType = "Not Have Permission";
                        errorDescription = "AppName = " + appName + " does not have permission to any services";
                        return false;
                    }
                    else if (entryApp.allowedServices.ToLower() != "All".ToLower())
                    {
                        string[] servicesArr = entryApp.allowedServices.Split(';');
                        int i;
                        for (i = 0; i <= servicesArr.Length - 1; i++)
                        {
                            if (servicesArr[i].Split(':')[0].ToLower() == "All".ToLower())
                            {
                                if (checkContent)
                                {
                                    if (servicesArr[i].Split(':')[1].ToLower() == "All".ToLower()
                                         || servicesArr[i].Split(':')[1].ToLower() == content.ToLower())
                                    {
                                        break;
                                    }
                                }
                                else break;
                            }

                            if (servicesArr[i].Split(':')[0] == serviceCode)
                            {
                                if (checkContent)
                                {
                                    if (servicesArr[i].Split(':')[1].ToLower() == "All".ToLower()
                                         || servicesArr[i].Split(':')[1].ToLower() == content.ToLower())
                                    {
                                        break;
                                    }
                                }
                                else break;
                            }
                        }
                        if (i == servicesArr.Length)
                        {
                            errorType = "Not Have Permission";
                            errorDescription = "AppName = " + appName + " does not have permission for Service Code = " + serviceCode
                                + " and Content = " + content;
                            return false;
                        }
                    }
                    return true;

                }



            }
            catch (Exception ex)
            {
                errorType = "Exception has been occured!!! Contact Administrator";
                errorDescription = "AppName = " + appName + " exception : " + ex.Message;
                return false;
            }
            return false;
        }

        public static string fnc_enryptAppParameter(string algorithm, string appName, int keySize, string IV, string keyVector
            , string plainText, out string errorType, out string errorDescription)
        {
            errorType = "";
            errorDescription = "";

            if (string.IsNullOrEmpty(plainText))
            {
                errorType = "PlaintText is not specified";
                return "";
            }

            try
            {
                string encryptedText = "";
                if (algorithm == "RijndaelManaged".ToLower())
                    encryptedText = EncryptString_RijndaelManaged(plainText, keySize, IV, keyVector);
                else if (algorithm.ToLower() == "AES".ToLower())
                    encryptedText = EncryptString_AES(plainText, keySize, IV, keyVector);
                return encryptedText;

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
