using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageArchive.Services
{
    public interface IAzureService
    {
        bool VerifyDbFirewallRules(string armResource, string tokenEndpoint, string spnPayload, string clientId, string tenantId, string clientSecret, string armUrl);
    }
}
