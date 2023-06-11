using System.Security.Cryptography;
using System.Text;

namespace qbot.Utility
{
    public static class SecurityPlayerPrefs
    {
        #region Fields
        private static readonly string saltForKey;
        private static readonly byte[] keys;
        private static readonly byte[] iv;
        private static readonly int keySize = 256;
        private static readonly int blockSize = 128;
        private static readonly int hashLen = 32;
        #endregion

        #region Constructor
        static SecurityPlayerPrefs()
        {
            /*
             * Be sure to include the values of [slatBytes], [randomSeedForKey], [and randomSeedForValue].
             * 
             * [Example] 
             * byte[] saltBytes = new byte[] { 36, 45, 11, 29, 94, 37, 85, 17 };
             * string randomSeedForKey = "5b6fcb4aaa0a42acae649eba45a506ec";
             * string randomSeedForValue = "24c79fmh24hfcaufc429cf824a8924mcfi";
             */

            // it must be 8 bytes
            var saltBytes = new byte[] { };

            // It doesn't matter the length, it's used to make keys
            var randomSeedForKey = "";

            // It doesn't matter the length, it's for creating keys and ivs for aes
            var randomSeedForValue = "";

            {
                var key = new Rfc2898DeriveBytes(randomSeedForKey, saltBytes, 1000);
                saltForKey = System.Convert.ToBase64String(key.GetBytes(blockSize / 8));
            }

            {
                var key = new Rfc2898DeriveBytes(randomSeedForValue, saltBytes, 1000);
                keys = key.GetBytes(keySize / 8);
                iv = key.GetBytes(blockSize / 8);
            }
        }
        #endregion

        #region Public functions
        public static void DeleteKey(string key)
        {
            UnityEngine.PlayerPrefs.DeleteKey(MakeHash(key + saltForKey));
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

        public static bool GetBool(string key, bool defaultValue)
        {
            var originalValue = GetSecurityValue(key);
            if (true == string.IsNullOrEmpty(originalValue))
                return defaultValue;

            if (false == bool.TryParse(originalValue, out var result))
                return defaultValue;

            return result;
        }

        public static int GetInt(string key, int defaultValue)
        {
            var originalValue = GetSecurityValue(key);
            if (true == string.IsNullOrEmpty(originalValue))
                return defaultValue;

            if (false == int.TryParse(originalValue, out var result))
                return defaultValue;

            return result;
        }

        public static long GetLong(string key, long defaultValue)
        {
            var originalValue = GetSecurityValue(key);
            if (true == string.IsNullOrEmpty(originalValue))
                return defaultValue;

            if (false == long.TryParse(originalValue, out var result))
                return defaultValue;

            return result;
        }

        public static float GetFloat(string key, float defaultValue)
        {
            var originalValue = GetSecurityValue(key);
            if (true == string.IsNullOrEmpty(originalValue))
                return defaultValue;

            if (false == float.TryParse(originalValue, out var result))
                return defaultValue;

            return result;
        }

        public static string GetString(string key, string defaultValue)
        {
            var originalValue = GetSecurityValue(key);

            if (true == string.IsNullOrEmpty(originalValue))
                return defaultValue;

            return originalValue;
        }
        #endregion

        #region Private functions
        public static string MakeHash(string original)
        {
            using var md5 = new MD5CryptoServiceProvider();
            var bytes = System.Text.Encoding.UTF8.GetBytes(original);
            var hashBytes = md5.ComputeHash(bytes);

            var hashToString = "";
            for (var i = 0; i < hashBytes.Length; ++i)
            {
                hashToString += hashBytes[i].ToString("x2");
            }

            return hashToString;
        }

        public static byte[] Encrypt(byte[] bytesToBeEncrypted)
        {
            using var aes = new RijndaelManaged();
            aes.KeySize = keySize;
            aes.BlockSize = blockSize;

            aes.Key = keys;
            aes.IV = iv;

            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var ct = aes.CreateEncryptor();
            return ct.TransformFinalBlock(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
        }

        public static byte[] Decrypt(byte[] bytesToBeDecrypted)
        {
            using var aes = new RijndaelManaged();

            aes.KeySize = keySize;
            aes.BlockSize = blockSize;

            aes.Key = keys;
            aes.IV = iv;

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
            var hideKey = MakeHash(key + saltForKey);
            var encryptValue = Encrypt(value + MakeHash(value));

            UnityEngine.PlayerPrefs.SetString(hideKey, encryptValue);
        }

        private static string GetSecurityValue(string key)
        {
            var hideKey = MakeHash(key + saltForKey);

            var encryptValue = UnityEngine.PlayerPrefs.GetString(hideKey);
            if (true == string.IsNullOrEmpty(encryptValue))
                return string.Empty;

            var valueAndHash = Decrypt(encryptValue);
            if (hashLen > valueAndHash.Length)
                return string.Empty;

            var savedValue = valueAndHash.Substring(0, valueAndHash.Length - hashLen);
            var savedHash = valueAndHash.Substring(valueAndHash.Length - hashLen);

            if (MakeHash(savedValue) != savedHash)
                return string.Empty;

            return savedValue;
        }
        #endregion
    }
}