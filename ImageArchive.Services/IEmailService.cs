using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageArchive.Services
{
    public interface IEmailService
    {
        void SendErrorEmail(string message);
        void SendErrorEmail(string message, Exception e);
    }
}
