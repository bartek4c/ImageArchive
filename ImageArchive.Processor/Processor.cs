using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Drawing;
using System.Windows.Media.Imaging;
using ImageArchive.Services;
using ImageArchive.Model.Enums;
using Newtonsoft.Json;
using ImageArchive.Model.ViewModels.Settings;

namespace ImageArchive.Processor
{
	public class Processor
	{
		//NAS
		private static string sourceDirectory;
		private static string imageDestinationDirectory;
		private static string videoDestinationDirectory;
		private static string psdDestinationDirectory;
		private static string processedDirectory;
		private static string couldNotProcessDirectory;
		private NetworkCredential _credentials;
		//Azure
		private static string armResource;
		private static string tokenEndpoint;
		private static string spnPayload;
		private static string clientId;
		private static string tenantId;
		private static string clientSecret;
		private static string armUrl;

		private ILoggingService _loggingService;
		private IAzureService _azureService;
		private IEmailService _emailService;
		private int _total = 0;

		public Processor()
		{
			var settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(@"C:\Settings\settings.txt"));

			//NAS
			sourceDirectory = settings.SourceDirectory;
			imageDestinationDirectory = settings.ImageDestinationDirectory;
			videoDestinationDirectory = settings.VideoDestinationDirectory;
			psdDestinationDirectory = settings.PsdDestinationDirectory;
			processedDirectory = settings.ProcessedDirectory;
			couldNotProcessDirectory = settings.CouldNotProcessDirectory;
			_credentials = new NetworkCredential(settings.NasAccessLogin, settings.NasAccessPassword);
			
			//Azure
			armResource = settings.ARMResource;
			tokenEndpoint = settings.TokenEndpoint;
			spnPayload = settings.SPNPayload;
			clientId = settings.ClientId;
			tenantId = settings.TenantId;
			clientSecret = settings.ClientSecret;
			armUrl = settings.ARMUrl;
			
			_loggingService = new LoggingService();
			_azureService = new AzureService();
			_emailService = new EmailService(settings.EmailFrom, settings.EmailTo, settings.SmtpServer, settings.SmtpPort, settings.SmtpLogin, settings.SmtpPassword);
		}

		public void RunProcessor()
		{
			try
			{
				//Check Azure db firewall rules (verify if the current IP has been listed)
				bool azureCorrect = _azureService.VerifyDbFirewallRules(armResource, tokenEndpoint, spnPayload, clientId, tenantId, clientSecret, armUrl);

				if (azureCorrect)
				{
					int logId = _loggingService.ProcessingStarted();

					//Process files in directory
					ProcessDirectory(sourceDirectory);
                    //Remove duplicates from db which have been removed locally
                    CheckForDeletedFiles();
                    //Delete empty directories					
                    DeleteDirectory(sourceDirectory);

					_loggingService.ProcessingFinished(_total, logId);
                    _total = 0;
				}
				else
				{
					//some issue with azure ARM
					_loggingService.ProcessingError("An issue has manifested when checking DB firewall rules", null);
					_emailService.SendErrorEmail("An issue has manifested when checking DB firewall rules");
				}
			}
			catch (Exception e)
			{
				_loggingService.ProcessingError("Exception has been thrown while processing files", e);
				_emailService.SendErrorEmail("Exception has been thrown while processing files", e);
			}
		}

		// Recursive method
		// Process all files in the directory passed in, recurse on any directories 
		// that are found, and process the files they contain.
		private void ProcessDirectory(string directory)
		{
			using (new NetworkConnection(directory, _credentials))
			{
				// Process the list of files found in the directory.
				string[] fileEntries = Directory.GetFiles(directory);
				foreach (string fileName in fileEntries)
				{
					ProcessFile(fileName);
					_total += 1;
				}
			}

			// Recurse into subdirectories of this directory.
			var subdirectoryEntries = Directory.GetDirectories(directory);
			foreach (string subdirectory in subdirectoryEntries)
				ProcessDirectory(subdirectory);
		}

        // Compare duplicates from db and delete each record that had it local copy deleted
        private void CheckForDeletedFiles() 
        {
            var recordsToDelete = new List<int>();

            var copies = _loggingService.GetCopies();
            
            foreach (KeyValuePair<int, string> item in copies)
            {
                string directory;
                switch (item.Value.Split('.')[1])
                {
                    case "jpg":
                    case "png":
                        directory = imageDestinationDirectory;
                        break;
                    case "psd":
                        directory = psdDestinationDirectory;
                        break;
                    default:
                        directory = videoDestinationDirectory;
                        break;
                }
                using (new NetworkConnection(directory, _credentials))
                {
                    if (!File.Exists(directory + item.Value))
                    {
                        recordsToDelete.Add(item.Key);
                    }
                }
            }

            _loggingService.DeletedRecords(recordsToDelete);
        }

        // Recursive method
        // Deletes all empty directories that are found at whichever level
        private void DeleteDirectory(string directory)
        {
            using (new NetworkConnection(directory, _credentials))
            {
                foreach (var d in Directory.GetDirectories(directory))
                {
                    DeleteDirectory(d);
                    if (Directory.GetFiles(d).Length == 0 && Directory.GetDirectories(d).Length == 0)
                    {
                        Directory.Delete(d, false);
                    }
                }
            }
        }

		/// <summary>
		/// Determine the type of file and destination directory
		/// </summary>
		/// <param name="path">file path</param>
		public void ProcessFile(string path)
		{
			var extension = Path.GetExtension(path).ToLower();

			if (extension == ".jpeg")
			{
				extension = ".jpg";
			}
			//process only images
			else if (extension == ".jpg" || extension == ".png")
			{
				ProcessFile(imageDestinationDirectory, path, extension, FileType.Image);
			}
			//process movies
			else if (extension == ".mov" || extension == ".mp4")
			{
				ProcessFile(videoDestinationDirectory, path, extension, FileType.Movie);
			}
			//process PSDs
			else if (extension == ".psd")
			{
				ProcessFile(psdDestinationDirectory, path, extension, FileType.PSD);
			}
			//other files
			else
			{
				ProcessFile(couldNotProcessDirectory, path, extension, FileType.Other);
			}
		}

		private void ProcessFile(string destinationFolder, string path, string extension, FileType fileType)
		{
			if (fileType != FileType.Other)
			{
				//produce new file and folder
				FileInfo info = new FileInfo(path);
				DateTime date = info.CreationTime;

				if (fileType == FileType.Image)
				{
					//metadata only for images
					using (FileStream fs = new FileStream(info.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
					{
						BitmapSource img = BitmapFrame.Create(fs);
						BitmapMetadata md = (BitmapMetadata)img.Metadata;
						if (md.DateTaken != null) date = Convert.ToDateTime(md.DateTaken);
					}
				}

				int numeral = 0;

				string fileFull;
				if (fileType == FileType.Image)
				{
					fileFull = BuildComplexFilePath(destinationFolder, extension, date, numeral);
				}
				else
				{
					fileFull = BuildSimplifiedFilePath(destinationFolder, extension, date, numeral);
				}

				//copy new file it the archive directory
				File.Copy(info.FullName, fileFull);
				//move the original file to the processed folder. Change the name if it exists
                var newLocation = Path.Combine(processedDirectory, info.Name);
				if (File.Exists(newLocation))
				{
					//do not change the name of psd files since the hidden file will be lost. Delete file in that case and then move
					if (info.Extension != ".psd")
					{
						File.Move(info.FullName, Path.Combine(processedDirectory, string.Format("{0}{1}{2}{3}{4}", info.Name.Split('.')[0], "_", DateTime.Now.ToString("yyyyMMddHHMMss"), "_", info.Extension)));
					}
					else
					{
						File.Delete(newLocation);
						File.Move(info.FullName, newLocation);
					}
				}
				else
				{
					File.Move(info.FullName, newLocation);
				}
				
				_loggingService.FileProcessed(
					extension,
					info.Name,
					fileFull.Substring(fileFull.LastIndexOf("\\") + 1, fileFull.Length - fileFull.LastIndexOf("\\") - 1),
					date.Year,
					date.Month);
			}
			else
			{
				FileInfo info = new FileInfo(path);
                var newLocation = Path.Combine(destinationFolder, info.Name);

                if (File.Exists(newLocation))
                {
                    File.Delete(newLocation);
                }
				File.Move(info.FullName, newLocation);

                _loggingService.FileCouldNotBeProcessed(newLocation);
			}
		}

		private string BuildComplexFilePath(string destinationFolder, string extension, DateTime date, int numeral)
		{
			string folder = Path.Combine(destinationFolder, date.ToString("yyyy"));
			string subfolder = Path.Combine(folder, date.ToString("MMMM"));
			string fileBase = Path.Combine(subfolder, date.ToString("yyyy/MM/dd HH:mm:ss")
				.Replace(" ", "_")
				.Replace("/", "")
				.Replace(":", "")) + "_";
			string fileFull = fileBase + numeral.ToString() + extension;

			//create folders structure and copy files
			if (!Directory.Exists(subfolder))
			{
				Directory.CreateDirectory(subfolder);
			}
			//check if file exists
			string newFileFull;
			VerifyFileName(fileFull, fileBase, numeral, extension, out newFileFull);
			return newFileFull;
		}

		private string BuildSimplifiedFilePath(string destinationFolder, string extension, DateTime date, int numeral)
		{
			string fileBase = Path.Combine(destinationFolder, date.ToString("yyyy/MM/dd HH:mm:ss")
				.Replace(" ", "_")
				.Replace("/", "")
				.Replace(":", "")) + "_";
			string fileFull = fileBase + numeral.ToString() + extension;

			//check if file exists
			string newFileFull;
			VerifyFileName(fileFull, fileBase, numeral, extension, out newFileFull);
			return newFileFull;
		}

		private void VerifyFileName(string fileFull, string fileBase, int numeral, string extension, out string newFileFull)
		{
			newFileFull = fileFull;
			if (File.Exists(fileFull))
			{
				numeral += 1;
				string nextFileFull = fileBase + numeral.ToString() + extension;
				VerifyFileName(nextFileFull, fileBase, numeral, extension, out newFileFull);
			}
		}
	}
}
