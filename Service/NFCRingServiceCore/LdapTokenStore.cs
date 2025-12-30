using System;
using System.Collections.Generic;
using System.DirectoryServices;
using Newtonsoft.Json;
using NFCRing.Service.Common;

namespace NFCRing.Service.Core
{
    public class LdapTokenStore
    {
        public bool Enabled => ServiceSettings.LdapEnabled && !string.IsNullOrWhiteSpace(ServiceSettings.LdapPath);

        public bool TryGetByToken(string token, out LdapTokenRecord record)
        {
            record = null;
            if (!Enabled || string.IsNullOrWhiteSpace(token))
                return false;

            using (var entry = CreateDirectoryEntry())
            using (var searcher = new DirectorySearcher(entry))
            {
                searcher.Filter = $"(&(objectClass=user)({ServiceSettings.LdapTokenAttribute}={EscapeFilterValue(token)}))";
                AddStandardProperties(searcher);

                var result = searcher.FindOne();
                if (result == null)
                    return false;

                record = BuildRecord(result, token);
                return record != null;
            }
        }

        public User GetUserState(string username)
        {
            if (!Enabled || string.IsNullOrWhiteSpace(username))
                return null;

            using (var entry = CreateDirectoryEntry())
            using (var searcher = new DirectorySearcher(entry))
            {
                searcher.Filter = $"(&(objectClass=user)({ServiceSettings.LdapUserAttribute}={EscapeFilterValue(username)}))";
                AddStandardProperties(searcher);

                var result = searcher.FindOne();
                if (result == null)
                    return null;

                var record = BuildRecord(result, null);
                if (record == null)
                    return null;

                return new User
                {
                    Username = record.Username,
                    Tokens = record.FriendlyNameMap ?? new Dictionary<string, string>(),
                    Events = new List<Event>(),
                    Salt = string.Empty
                };
            }
        }

        public bool RegisterToken(string username, string token, string friendlyName)
        {
            if (!Enabled || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(token))
                return false;

            using (var entry = FindUserEntry(username))
            {
                if (entry == null)
                    return false;

                EnsureTokenValue(entry, token);

                var friendlyMap = LoadMap(entry, ServiceSettings.LdapFriendlyNameMapAttribute);
                friendlyMap[token] = friendlyName ?? string.Empty;
                SaveMap(entry, ServiceSettings.LdapFriendlyNameMapAttribute, friendlyMap);

                entry.CommitChanges();
                return true;
            }
        }

        public bool StoreCredential(string username, string token, string encryptedPassword)
        {
            if (!Enabled || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(token))
                return false;

            using (var entry = FindUserEntry(username))
            {
                if (entry == null)
                    return false;

                EnsureTokenValue(entry, token);

                var passwordMap = LoadMap(entry, ServiceSettings.LdapPasswordMapAttribute);
                passwordMap[token] = encryptedPassword ?? string.Empty;
                SaveMap(entry, ServiceSettings.LdapPasswordMapAttribute, passwordMap);

                entry.CommitChanges();
                return true;
            }
        }

        public bool UpdateFriendlyName(string username, string token, string friendlyName)
        {
            if (!Enabled || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(token))
                return false;

            using (var entry = FindUserEntry(username))
            {
                if (entry == null)
                    return false;

                var friendlyMap = LoadMap(entry, ServiceSettings.LdapFriendlyNameMapAttribute);
                if (!friendlyMap.ContainsKey(token))
                    return false;

                friendlyMap[token] = friendlyName ?? string.Empty;
                SaveMap(entry, ServiceSettings.LdapFriendlyNameMapAttribute, friendlyMap);

                entry.CommitChanges();
                return true;
            }
        }

        public bool RemoveToken(string username, string token)
        {
            if (!Enabled || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(token))
                return false;

            using (var entry = FindUserEntry(username))
            {
                if (entry == null)
                    return false;

                RemoveTokenValue(entry, token);

                var friendlyMap = LoadMap(entry, ServiceSettings.LdapFriendlyNameMapAttribute);
                friendlyMap.Remove(token);
                SaveMap(entry, ServiceSettings.LdapFriendlyNameMapAttribute, friendlyMap);

                var passwordMap = LoadMap(entry, ServiceSettings.LdapPasswordMapAttribute);
                passwordMap.Remove(token);
                SaveMap(entry, ServiceSettings.LdapPasswordMapAttribute, passwordMap);

                entry.CommitChanges();
                return true;
            }
        }

        private DirectoryEntry CreateDirectoryEntry()
        {
            if (string.IsNullOrWhiteSpace(ServiceSettings.LdapBindUser))
            {
                return new DirectoryEntry(ServiceSettings.LdapPath);
            }

            return new DirectoryEntry(ServiceSettings.LdapPath, ServiceSettings.LdapBindUser, ServiceSettings.LdapBindPassword);
        }

        private DirectoryEntry FindUserEntry(string username)
        {
            using (var entry = CreateDirectoryEntry())
            using (var searcher = new DirectorySearcher(entry))
            {
                searcher.Filter = $"(&(objectClass=user)({ServiceSettings.LdapUserAttribute}={EscapeFilterValue(username)}))";
                searcher.PropertiesToLoad.Add(ServiceSettings.LdapUserAttribute);
                var result = searcher.FindOne();
                return result?.GetDirectoryEntry();
            }
        }

        private static void AddStandardProperties(DirectorySearcher searcher)
        {
            searcher.PropertiesToLoad.Add(ServiceSettings.LdapUserAttribute);
            searcher.PropertiesToLoad.Add("userPrincipalName");
            searcher.PropertiesToLoad.Add(ServiceSettings.LdapFriendlyNameMapAttribute);
            searcher.PropertiesToLoad.Add(ServiceSettings.LdapPasswordMapAttribute);
            searcher.PropertiesToLoad.Add(ServiceSettings.LdapTokenAttribute);
        }

        private static LdapTokenRecord BuildRecord(SearchResult result, string token)
        {
            var username = GetProperty(result, ServiceSettings.LdapUserAttribute)
                ?? GetProperty(result, "sAMAccountName")
                ?? GetProperty(result, "userPrincipalName");

            if (string.IsNullOrWhiteSpace(username))
                return null;

            var friendlyMap = LoadMap(result, ServiceSettings.LdapFriendlyNameMapAttribute);
            var passwordMap = LoadMap(result, ServiceSettings.LdapPasswordMapAttribute);

            var record = new LdapTokenRecord
            {
                Username = username,
                Domain = ServiceSettings.LdapDomain,
                FriendlyNameMap = friendlyMap,
                PasswordMap = passwordMap
            };

            if (!string.IsNullOrWhiteSpace(token))
            {
                friendlyMap.TryGetValue(token, out var friendlyName);
                passwordMap.TryGetValue(token, out var encryptedPassword);
                record.FriendlyName = friendlyName;
                record.EncryptedPassword = encryptedPassword;
            }

            return record;
        }

        private static string GetProperty(SearchResult result, string name)
        {
            if (result.Properties.Contains(name) && result.Properties[name].Count > 0)
            {
                return result.Properties[name][0]?.ToString();
            }

            return null;
        }

        private static Dictionary<string, string> LoadMap(SearchResult result, string attribute)
        {
            if (result.Properties.Contains(attribute) && result.Properties[attribute].Count > 0)
            {
                var value = result.Properties[attribute][0]?.ToString();
                return DeserializeMap(value);
            }

            return new Dictionary<string, string>();
        }

        private static Dictionary<string, string> LoadMap(DirectoryEntry entry, string attribute)
        {
            if (entry.Properties.Contains(attribute) && entry.Properties[attribute].Value != null)
            {
                var value = entry.Properties[attribute].Value.ToString();
                return DeserializeMap(value);
            }

            return new Dictionary<string, string>();
        }

        private static Dictionary<string, string> DeserializeMap(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new Dictionary<string, string>();

            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(value)
                       ?? new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        private static void SaveMap(DirectoryEntry entry, string attribute, Dictionary<string, string> map)
        {
            entry.Properties[attribute].Value = JsonConvert.SerializeObject(map ?? new Dictionary<string, string>());
        }

        private static void EnsureTokenValue(DirectoryEntry entry, string token)
        {
            var tokens = entry.Properties[ServiceSettings.LdapTokenAttribute];
            var exists = false;
            foreach (var value in tokens)
            {
                if (string.Equals(value?.ToString(), token, StringComparison.OrdinalIgnoreCase))
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
                tokens.Add(token);
        }

        private static void RemoveTokenValue(DirectoryEntry entry, string token)
        {
            var tokens = entry.Properties[ServiceSettings.LdapTokenAttribute];
            for (var i = tokens.Count - 1; i >= 0; i--)
            {
                if (string.Equals(tokens[i]?.ToString(), token, StringComparison.OrdinalIgnoreCase))
                {
                    tokens.RemoveAt(i);
                }
            }
        }

        private static string EscapeFilterValue(string value)
        {
            return value
                .Replace("\\", "\\5c")
                .Replace("*", "\\2a")
                .Replace("(", "\\28")
                .Replace(")", "\\29")
                .Replace("\u0000", "\\00");
        }
    }

    public class LdapTokenRecord
    {
        public string Username { get; set; }
        public string Domain { get; set; }
        public string EncryptedPassword { get; set; }
        public string FriendlyName { get; set; }
        public Dictionary<string, string> FriendlyNameMap { get; set; }
        public Dictionary<string, string> PasswordMap { get; set; }
    }
}
