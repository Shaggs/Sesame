using System;
using System.Configuration;
using System.Net;

namespace NFCRing.Service.Common
{
    public static class NetworkSettings
    {
        public const int DefaultCredentialPort = 28416;
        public const int DefaultRegistrationPort = 28417;

        public static string ServiceHost
        {
            get { return GetString("NFCRing.ServiceHost", "127.0.0.1"); }
        }

        public static IPAddress CredentialBindAddress
        {
            get { return GetIPAddress("NFCRing.CredentialBindAddress", IPAddress.Loopback); }
        }

        public static IPAddress RegistrationBindAddress
        {
            get { return GetIPAddress("NFCRing.RegistrationBindAddress", IPAddress.Loopback); }
        }

        public static int CredentialPort
        {
            get { return GetInt("NFCRing.CredentialPort", DefaultCredentialPort); }
        }

        public static int RegistrationPort
        {
            get { return GetInt("NFCRing.RegistrationPort", DefaultRegistrationPort); }
        }

        public static bool EnableRemoteTokenLookup
        {
            get { return GetBool("NFCRing.EnableRemoteTokenLookup", true); }
        }

        public static bool EnableActiveDirectoryCardAttributes
        {
            get { return GetBool("NFCRing.EnableActiveDirectoryCardAttributes", false); }
        }

        public static string ActiveDirectoryCardAttribute
        {
            get { return GetString("NFCRing.ADCardAttribute", "extensionAttribute10"); }
        }

        public static string ActiveDirectoryCardHashSalt
        {
            get { return GetString("NFCRing.ADCardHashSalt", ""); }
        }

        public static bool IsRemoteServiceHostConfigured
        {
            get
            {
                return !String.Equals(ServiceHost, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
                    && !String.Equals(ServiceHost, "localhost", StringComparison.OrdinalIgnoreCase)
                    && !String.Equals(ServiceHost, "::1", StringComparison.OrdinalIgnoreCase);
            }
        }

        private static string GetString(string key, string fallback)
        {
            string value = ConfigurationManager.AppSettings[key];
            return String.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static int GetInt(string key, int fallback)
        {
            int value;
            return Int32.TryParse(ConfigurationManager.AppSettings[key], out value) ? value : fallback;
        }

        private static bool GetBool(string key, bool fallback)
        {
            bool value;
            return Boolean.TryParse(ConfigurationManager.AppSettings[key], out value) ? value : fallback;
        }

        private static IPAddress GetIPAddress(string key, IPAddress fallback)
        {
            string value = ConfigurationManager.AppSettings[key];
            if (String.IsNullOrWhiteSpace(value))
                return fallback;

            if (String.Equals(value, "Any", StringComparison.OrdinalIgnoreCase))
                return IPAddress.Any;

            if (String.Equals(value, "Loopback", StringComparison.OrdinalIgnoreCase))
                return IPAddress.Loopback;

            IPAddress address;
            return IPAddress.TryParse(value, out address) ? address : fallback;
        }
    }
}
