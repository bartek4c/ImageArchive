using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageArchive.Model.ViewModels.Settings
{
    public class Settings
    {
        //NAS
        public string SourceDirectory { get; set; }
        public string ImageDestinationDirectory { get; set; }
        public string VideoDestinationDirectory { get; set; }
        public string PsdDestinationDirectory { get; set; }
        public string ProcessedDirectory { get; set; }
        public string CouldNotProcessDirectory { get; set; }
        public string NasAccessLogin { get; set; }
        public string NasAccessPassword { get; set; }
        //Azure
        public string ARMResource { get; set; }
        public string TokenEndpoint { get; set; }
        public string SPNPayload { get; set; }
        public string ClientId { get; set; }
        public string TenantId { get; set; }
        public string ClientSecret { get; set; }
        public string ARMUrl { get; set; }
        //Email
        public string EmailFrom { get; set; }
        public string EmailTo { get; set; }
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpLogin { get; set; }
        public string SmtpPassword { get; set; }
    }
}


