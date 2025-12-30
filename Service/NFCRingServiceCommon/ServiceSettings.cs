using System;
using System.Configuration;

namespace NFCRing.Service.Common
{
    public static class ServiceSettings
    {
        public static string RegistrationHost => GetString("NFCRing.RegistrationHost", "127.0.0.1");
        public static int RegistrationPort => GetInt("NFCRing.RegistrationPort", 28417);
        public static int CredentialPort => GetInt("NFCRing.CredentialPort", 28416);

        public static bool LdapEnabled => GetBool("NFCRing.Ldap.Enabled", false);
        public static string LdapPath => GetString("NFCRing.Ldap.Path", string.Empty);
        public static string LdapBindUser => GetString("NFCRing.Ldap.BindUser", string.Empty);
        public static string LdapBindPassword => GetString("NFCRing.Ldap.BindPassword", string.Empty);
        public static string LdapTokenAttribute => GetString("NFCRing.Ldap.TokenAttribute", "extensionAttribute1");
        public static string LdapPasswordMapAttribute => GetString("NFCRing.Ldap.PasswordMapAttribute", "extensionAttribute2");
        public static string LdapFriendlyNameMapAttribute => GetString("NFCRing.Ldap.FriendlyNameMapAttribute", "extensionAttribute3");
        public static string LdapUserAttribute => GetString("NFCRing.Ldap.UserAttribute", "sAMAccountName");
        public static string LdapDomain => GetString("NFCRing.Ldap.Domain", string.Empty);
        public static string LdapUnlockPluginName => GetString("NFCRing.Ldap.UnlockPlugin", "Unlock Workstation (network)");
        public static string LdapLockPluginName => GetString("NFCRing.Ldap.LockPlugin", "Lock Workstation");
        public static bool LdapLockOnRemove => GetBool("NFCRing.Ldap.LockOnRemove", false);

        private static string GetString(string key, string defaultValue)
        {
            var value = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
        }

        private static int GetInt(string key, int defaultValue)
        {
            var value = ConfigurationManager.AppSettings[key];
            return int.TryParse(value, out var result) ? result : defaultValue;
        }

        private static bool GetBool(string key, bool defaultValue)
        {
            var value = ConfigurationManager.AppSettings[key];
            return bool.TryParse(value, out var result) ? result : defaultValue;
        }
    }
}
