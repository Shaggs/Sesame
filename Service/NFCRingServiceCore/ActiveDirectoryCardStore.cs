using System;
using System.DirectoryServices;
using NFCRing.Service.Common;

namespace NFCRing.Service.Core
{
    internal static class ActiveDirectoryCardStore
    {
        public static string GetCardHash(string rawToken)
        {
            string tokenHash = Crypto.Hash(rawToken);
            string salt = NetworkSettings.ActiveDirectoryCardHashSalt;
            return String.IsNullOrEmpty(salt) ? tokenHash : Crypto.Hash(tokenHash + salt);
        }

        public static void AssignCardToUser(string username, string rawToken)
        {
            if (!NetworkSettings.EnableActiveDirectoryCardAttributes)
                return;

            using (DirectoryEntry userEntry = FindUser(username))
            {
                if (userEntry == null)
                    throw new InvalidOperationException("Unable to find AD user " + username);

                userEntry.Properties[NetworkSettings.ActiveDirectoryCardAttribute].Value = GetCardHash(rawToken);
                userEntry.CommitChanges();
            }
        }

        public static string FindUsernameByCard(string rawToken)
        {
            if (!NetworkSettings.EnableActiveDirectoryCardAttributes)
                return null;

            string cardHash = GetCardHash(rawToken);
            string attribute = NetworkSettings.ActiveDirectoryCardAttribute;

            using (DirectoryEntry root = new DirectoryEntry())
            using (DirectorySearcher searcher = new DirectorySearcher(root))
            {
                searcher.Filter = "(&(objectCategory=person)(objectClass=user)(" + attribute + "=" + EscapeLdapFilterValue(cardHash) + "))";
                searcher.PropertiesToLoad.Add("sAMAccountName");
                searcher.PropertiesToLoad.Add("userPrincipalName");
                searcher.PropertiesToLoad.Add("distinguishedName");

                SearchResult result = searcher.FindOne();
                if (result == null)
                    return null;

                string samAccountName = GetProperty(result, "sAMAccountName");
                if (String.IsNullOrEmpty(samAccountName))
                    return GetProperty(result, "userPrincipalName");

                string domain = GetDomainName(result);
                return String.IsNullOrEmpty(domain) ? samAccountName : domain + "\\" + samAccountName;
            }
        }

        private static DirectoryEntry FindUser(string username)
        {
            string accountName = username;
            int slashIndex = username.LastIndexOf('\\');
            if (slashIndex > -1 && slashIndex < username.Length - 1)
                accountName = username.Substring(slashIndex + 1);

            using (DirectoryEntry root = new DirectoryEntry())
            using (DirectorySearcher searcher = new DirectorySearcher(root))
            {
                string escapedUsername = EscapeLdapFilterValue(username);
                string escapedAccountName = EscapeLdapFilterValue(accountName);
                searcher.Filter = "(&(objectCategory=person)(objectClass=user)(|(sAMAccountName="
                    + escapedAccountName + ")(userPrincipalName=" + escapedUsername + ")))";
                SearchResult result = searcher.FindOne();
                return result == null ? null : result.GetDirectoryEntry();
            }
        }

        private static string GetProperty(SearchResult result, string propertyName)
        {
            return result.Properties.Contains(propertyName) && result.Properties[propertyName].Count > 0
                ? result.Properties[propertyName][0].ToString()
                : "";
        }

        private static string GetDomainName(SearchResult result)
        {
            string distinguishedName = GetProperty(result, "distinguishedName");
            if (String.IsNullOrEmpty(distinguishedName))
                return "";

            string[] parts = distinguishedName.Split(',');
            string domain = "";
            foreach (string part in parts)
            {
                string trimmed = part.Trim();
                if (trimmed.StartsWith("DC=", StringComparison.OrdinalIgnoreCase))
                {
                    if (domain.Length > 0)
                        domain += ".";
                    domain += trimmed.Substring(3);
                }
            }

            return domain;
        }

        private static string EscapeLdapFilterValue(string value)
        {
            return value
                .Replace("\\", "\\5c")
                .Replace("*", "\\2a")
                .Replace("(", "\\28")
                .Replace(")", "\\29")
                .Replace("\0", "\\00");
        }
    }
}
