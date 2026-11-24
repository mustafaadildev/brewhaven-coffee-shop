using System;
using System.Configuration;
using System.Security.Cryptography;

namespace BrewHaven.Web.Security
{
    /// <summary>
    /// Verifies the owner passcode against the PBKDF2 hash stored in
    /// Web.config (appSettings → AdminPasscodeHash). The passcode itself is
    /// never stored — the config value is "PBKDF2|iterations|saltHex|hashBase64",
    /// generated with Rfc2898DeriveBytes(..., HashAlgorithmName.SHA256).
    /// </summary>
    public static class AdminPasscode
    {
        public static bool Verify(string passcode)
        {
            if (string.IsNullOrEmpty(passcode))
            {
                return false;
            }

            var setting = ConfigurationManager.AppSettings["AdminPasscodeHash"];
            if (string.IsNullOrEmpty(setting))
            {
                return false;
            }

            var parts = setting.Split('|');
            if (parts.Length != 4 || parts[0] != "PBKDF2")
            {
                return false;
            }

            int iterations;
            if (!int.TryParse(parts[1], out iterations) || iterations < 10000)
            {
                return false;
            }

            byte[] salt;
            byte[] expected;
            try
            {
                salt = HexToBytes(parts[2]);
                expected = Convert.FromBase64String(parts[3]);
            }
            catch (FormatException)
            {
                return false;
            }

            using (var derive = new Rfc2898DeriveBytes(passcode, salt, iterations, HashAlgorithmName.SHA256))
            {
                return FixedTimeEquals(derive.GetBytes(expected.Length), expected);
            }
        }

        /// <summary>Constant-time comparison so response timing leaks nothing.</summary>
        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            int diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }
            return diff == 0;
        }

        private static byte[] HexToBytes(string hex)
        {
            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }
    }
}
