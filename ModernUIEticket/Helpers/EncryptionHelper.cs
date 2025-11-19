using System;
using System.Text;

namespace ETicketNewUI.Helpers
{
    public static class EncryptionHelper
    {
        // Encode (Base64 for example)
        public static string EncodeKey(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var bytes = Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(bytes);
        }

        // Decode back
        public static string DecodeKey(string encoded)
        {
            if (string.IsNullOrEmpty(encoded))
                return string.Empty;

            var bytes = Convert.FromBase64String(encoded);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
