using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace PetWAMS.Web.Services
{
    public interface INidProtectionService
    {
        string NormalizeNid(string nidNumber);
        string EncryptNid(string nidNumber);
        string DecryptNid(string encryptedNid);
        string HashNid(string nidNumber);
        string GetLastFour(string nidNumber);
    }

    public class NidProtectionService : INidProtectionService
    {
        private readonly IDataProtector _protector;

        public NidProtectionService(
            IDataProtectionProvider dataProtectionProvider)
        {
            _protector = dataProtectionProvider.CreateProtector(
                "PetWAMS.AdopterVerification.NID.v1");
        }

        public string NormalizeNid(string nidNumber)
        {
            if (string.IsNullOrWhiteSpace(nidNumber))
            {
                throw new ArgumentException(
                    "NID number is required.",
                    nameof(nidNumber));
            }

            string normalizedNid = new string(
                nidNumber.Where(char.IsDigit).ToArray());

            if (string.IsNullOrWhiteSpace(normalizedNid))
            {
                throw new ArgumentException(
                    "NID number must contain digits.",
                    nameof(nidNumber));
            }

            return normalizedNid;
        }

        public string EncryptNid(string nidNumber)
        {
            string normalizedNid = NormalizeNid(nidNumber);
            return _protector.Protect(normalizedNid);
        }

        public string DecryptNid(string encryptedNid)
        {
            if (string.IsNullOrWhiteSpace(encryptedNid))
            {
                throw new ArgumentException(
                    "Encrypted NID is required.",
                    nameof(encryptedNid));
            }

            return _protector.Unprotect(encryptedNid);
        }

        public string HashNid(string nidNumber)
        {
            string normalizedNid = NormalizeNid(nidNumber);
            byte[] nidBytes = Encoding.UTF8.GetBytes(normalizedNid);
            byte[] hashBytes = SHA256.HashData(nidBytes);

            return Convert.ToHexString(hashBytes);
        }

        public string GetLastFour(string nidNumber)
        {
            string normalizedNid = NormalizeNid(nidNumber);

            if (normalizedNid.Length <= 4)
            {
                return normalizedNid;
            }

            return normalizedNid[^4..];
        }
    }
}