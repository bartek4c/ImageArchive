using ImageArchive.DataAccess.Model;
using ImageArchive.Model.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageArchive.Services
{
    public interface ILoggingService
    {
        void FileProcessed(string extension, string originalName, string newName, int? year, int? month);
        void FileCouldNotBeProcessed(string originalName);
        int ProcessingStarted();
        void ProcessingFinished(int totalProcessed, int logId);
        void ProcessingError(string message, Exception e);
    }
}
