using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace qbot.Utility
{
    public static class SecurityPlayerPrefs
    {
        private static readonly string SaltForKey;
        private static readonly byte[] Keys;
        private static readonly byte[] Iv;
        private const int KeySize = 256;
        private const int BlockSize = 128;
        private const int HashLen = 32;

        static SecurityPlayerPrefs()
        {
            /*
             * Be sure to include the values of [slatBytes], [randomSeedForKey], [and randomSeedForValue].
             *
             * [Example]
             * var saltBytes = new byte[] { 36, 45, 11, 29, 94, 37, 85, 17 };
             * var randomSeedForKey = "5b6fcb4aaa0a42acae649eba45a506ec";
             * var randomSeedForValue = "24c79fmh24hfcaufc429cf824a8924mcfi";
             */

            // it must be 8 bytes
            var saltBytes = new byte[] { };

            // It doesn't matter the length, it's used to make keys
            var randomSeedForKey = "";

            // It doesn't matter the length, it's for creating keys and ivs for aes
            var randomSeedForValue = "";

            {
                var key = new Rfc2898DeriveBytes(RandomSeedForKey, saltBytes, 1000);
                SaltForKey = System.Convert.ToBase64String(key.GetBytes(BlockSize / 8));
            }

            {
                var key = new Rfc2898DeriveBytes(RandomSeedForValue, saltBytes, 1000);
                Keys = key.GetBytes(KeySize / 8);
                Iv = key.GetBytes(BlockSize / 8);
            }
        }

        public static void DeleteKey(string key)
        {
            UnityEngine.PlayerPrefs.DeleteKey(MakeHash(key + SaltForKey));
        }

        public static void DeleteAll()
        {
            UnityEngine.PlayerPrefs.DeleteAll();
        }

        public static void Save()
        {
            UnityEngine.PlayerPrefs.Save();
        }

        public static void SetBool(string key, bool value)
        {
            SetSecurityValue(key, value.ToString());
        }

        public static void SetInt(string key, int value)
        {
            SetSecurityValue(key, value.ToString());
        }

        public static void SetLong(string key, long value)
        {
            SetSecurityValue(key, value.ToString());
        }

        public static void SetFloat(string key, float value)
        {
            SetSecurityValue(key, value.ToString());
        }

        public static void SetString(string key, string value)
        {
            SetSecurityValue(key, value);
        }

        public static void SetList(string key, List<object> value)
        {
            var listValue = JsonConvert.SerializeObject(value);
            SetSecurityValue(key, listValue);
        }

        public static bool GetBool(string key, bool defaultValue = false)
        {
            var originalValue = GetSecurityValue(key);
            if (string.IsNullOrEmpty(originalValue))
                return defaultValue;

            if (false == bool.TryParse(originalValue, out var result))
                return defaultValue;

            return result;
        }

        public static int GetInt(string key, int defaultValue = default)
        {
            var originalValue = GetSecurityValue(key);
            if (string.IsNullOrEmpty(originalValue))
                return defaultValue;

            if (false == int.TryParse(originalValue, out var result))
                return defaultValue;

            return result;
        }

        public static long GetLong(string key, long defaultValue = default)
        {
            var originalValue = GetSecurityValue(key);
            if (string.IsNullOrEmpty(originalValue))
                return defaultValue;

            if (false == long.TryParse(originalValue, out var result))
                return defaultValue;

            return result;
        }

        public static float GetFloat(string key, float defaultValue = default)
        {
            var originalValue = GetSecurityValue(key);
            if (string.IsNullOrEmpty(originalValue))
                return defaultValue;

            if (false == float.TryParse(originalValue, out var result))
                return defaultValue;

            return result;
        }

        public static string GetString(string key, string defaultValue = default)
        {
            var originalValue = GetSecurityValue(key);

            if (string.IsNullOrEmpty(originalValue))
                return defaultValue;

            return originalValue;
        }
        
        public static List<object> GetList(string key)
        {
            var originValue = GetSecurityValue(key);
            var listValue = JsonConvert.DeserializeObject<List<object>>(originValue);
            
            return listValue;
        }

        public static string MakeHash(string original)
        {
            using var md5 = new MD5CryptoServiceProvider();
            var bytes = Encoding.UTF8.GetBytes(original);
            var hashBytes = md5.ComputeHash(bytes);

            var hashToString = "";
            foreach (var t in hashBytes)
            {
                hashToString += t.ToString("x2");
            }

            return hashToString;
        }

        public static byte[] Encrypt(byte[] bytesToBeEncrypted)
        {
            using var aes = new RijndaelManaged();
            aes.KeySize = KeySize;
            aes.BlockSize = BlockSize;

            aes.Key = Keys;
            aes.IV = Iv;

            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var ct = aes.CreateEncryptor();
            return ct.TransformFinalBlock(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
        }

        public static byte[] Decrypt(byte[] bytesToBeDecrypted)
        {
            using var aes = new RijndaelManaged();

            aes.KeySize = KeySize;
            aes.BlockSize = BlockSize;

            aes.Key = Keys;
            aes.IV = Iv;

            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var ct = aes.CreateDecryptor();
            return ct.TransformFinalBlock(bytesToBeDecrypted, 0, bytesToBeDecrypted.Length);
        }

        public static string Encrypt(string input)
        {
            var bytesToBeEncrypted = Encoding.UTF8.GetBytes(input);
            var bytesEncrypted = Encrypt(bytesToBeEncrypted);

            return System.Convert.ToBase64String(bytesEncrypted);
        }

        public static string Decrypt(string input)
        {
            var bytesToBeDecrypted = System.Convert.FromBase64String(input);
            var bytesDecrypted = Decrypt(bytesToBeDecrypted);

            return Encoding.UTF8.GetString(bytesDecrypted);
        }

        private static void SetSecurityValue(string key, string value)
        {
            var hideKey = MakeHash(key + SaltForKey);
            var encryptValue = Encrypt(value + MakeHash(value));

            UnityEngine.PlayerPrefs.SetString(hideKey, encryptValue);
        }

        private static string GetSecurityValue(string key)
        {
            var hideKey = MakeHash(key + SaltForKey);

            var encryptValue = UnityEngine.PlayerPrefs.GetString(hideKey);
            if (string.IsNullOrEmpty(encryptValue))
                return string.Empty;

            var valueAndHash = Decrypt(encryptValue);
            if (HashLen > valueAndHash.Length)
                return string.Empty;

            var savedValue = valueAndHash.Substring(0, valueAndHash.Length - HashLen);
            var savedHash = valueAndHash.Substring(valueAndHash.Length - HashLen);

            if (MakeHash(savedValue) != savedHash)
                return string.Empty;

            return savedValue;
        }
    }
}