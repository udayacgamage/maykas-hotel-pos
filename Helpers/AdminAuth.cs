using System;
using System.Security.Cryptography;
using System.Text;

namespace HotelPOS.Wpf.Helpers
{
    public static class AdminAuth
    {
        // Default PIN is "1234" (SHA-256). Change this to your own PIN hash.
        private const string AdminPinSha256Hex = "03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4";

        public static bool VerifyPin(string? pin)
        {
            pin ??= "";
            var bytes = Encoding.UTF8.GetBytes(pin);
            var hash = SHA256.HashData(bytes);
            var hex = Convert.ToHexString(hash).ToLowerInvariant();
            return string.Equals(hex, AdminPinSha256Hex, StringComparison.Ordinal);
        }
    }
}
