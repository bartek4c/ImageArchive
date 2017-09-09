using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace ImageArchive.ConsoleApp
{
	class Program
	{
		//entry point to the processr from console app
		static void Main(string[] args)
		{   
			Console.WriteLine("started");
            var processor = new ImageArchive.Processor.Processor();
			processor.RunProcessor();
            Console.WriteLine("finished");
		}

		
	}
}
