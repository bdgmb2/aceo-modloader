using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace ModLoaderLibrary
{
    [Serializable]
    class GitHubRelease
    {
        public string tag_name;
        public string name;
        public string url;
    }

    class MonoNetwork
    {
        public static bool RemoteCertValidator(Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            LogOutput.Log("Validating Certificate...");
            bool validated = true;
            if (sslPolicyErrors != SslPolicyErrors.None)
            {
                foreach (var status in chain.ChainStatus)
                {
                    if (status.Status == X509ChainStatusFlags.RevocationStatusUnknown)
                    {
                        continue;
                    }

                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                    chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
                    bool chainIsValid = chain.Build((X509Certificate2)certificate);
                    if (!chainIsValid)
                    {
                        validated = false;
                        break;
                    }
                }
            }

            return validated;
        }
    }
}
