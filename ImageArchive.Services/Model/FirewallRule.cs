using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageArchive.Services.Model
{
    public class FirewallRule
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Location { get; set; }
        public string Kind { get; set; }
        public Properties Properties { get; set; }
    }
}
