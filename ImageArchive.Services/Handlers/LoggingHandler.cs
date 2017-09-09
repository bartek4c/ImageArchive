using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ImageArchive.Services.Handlers
{
    public class LoggingHandler : DelegatingHandler
    {
        public LoggingHandler(HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {   
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);   
            return response;
        }
    }
}
