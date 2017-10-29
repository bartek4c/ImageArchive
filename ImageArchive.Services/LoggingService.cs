using ImageArchive.DataAccess;
using ImageArchive.DataAccess.Model;
using ImageArchive.DataAccess.Model.Enums;
using ImageArchive.Model.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageArchive.Services
{
    public class LoggingService : ILoggingService
    {
        public void FileProcessed(string extension, string originalName, string newName, int? year = null, int? month = null)
        {
            using (var db = new ImageArchiveContext())
            {
                //check if there are any exisitng items with the same id (could be already created and then deleted as duplicates)
                var item = db.ArchivedImages.FirstOrDefault(x => x.ImageId == newName);

                if (item == null)
                {
                    item = new ArchivedImage();
                    var populatedItem = MapObject(item, extension, originalName, newName, year, month);
                    db.ArchivedImages.Add(populatedItem);
                    db.SaveChanges();
                }
                else
                {
                    item = MapObject(item, extension, originalName, newName, year, month);
                    db.SaveChanges();
                }
            }
        }

        public void DeletedRecords(List<int> ids)
        {
            using (var db = new ImageArchiveContext())
            {
                foreach (var id in ids)
                {
                    var file = db.ArchivedImages.FirstOrDefault(x => x.Id == id);
                    db.ArchivedImages.Remove(file);
                    db.SaveChanges();
                }
            }
        }

        private ArchivedImage MapObject(ArchivedImage item, string extension, string originalName, string newName, int? year = null, int? month = null)
        {
            item.Timestamp = DateTime.Now;
            item.Extension = extension;
            item.OriginalName = originalName;
            item.ImageId = newName;
            item.Year = year;
            item.Month = month;
            return item;
        }

        public Dictionary<int, string> GetCopies()
        {
            Dictionary<int, string> copies = new Dictionary<int,string>();
            using (var db = new ImageArchiveContext())
            {
                var rejectList = db.ArchivedImages.Where(x => x.ImageId.Contains("_0."));
                foreach (var copy in db.ArchivedImages.Except(rejectList))
                {
                    string path;
                    if (copy.Extension == ".jpg" || copy.Extension == ".png")
                    {
                        path = string.Format("\\{0}\\{1}\\{2}", copy.Year, CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Convert.ToInt16(copy.Month)), copy.ImageId);
                    }
                    else 
                    {
                        path = string.Format("\\{0}", copy.ImageId);
                    }
                    copies.Add(copy.Id, path);
                }
            }
            return copies;
        }
        
        public void FileCouldNotBeProcessed(string originalName)
        {
            using (var db = new ImageArchiveContext())
            {                
                var log = new Log
                {
                    StartDate = DateTime.Now,
                    Type = LogType.CouldNotProcess,
                    Message = "The file could not be processed",
                    FileName = originalName
                };
                db.Logs.Add(log);
                db.SaveChanges();
            }
        }

        public int ProcessingStarted()
        {
            using (var db = new ImageArchiveContext())
            {
                var log = new Log {
                    StartDate = DateTime.Now,
                    Type = LogType.Info,
                    Message = "Image processing new batch started"
                };
                db.Logs.Add(log);
                db.SaveChanges();
                return log.Id;
            }
        }

        public void ProcessingFinished(int totalProcessed, int logId)
        {
            using (var db = new ImageArchiveContext())
            {
                var log = db.Logs.FirstOrDefault(x => x.Id == logId);
                log.Message = string.Format("Processing files successfully completed - {0} files processed", totalProcessed);
                log.FilesProcessed = totalProcessed;
                log.EndDate = DateTime.Now;
                db.SaveChanges();
            }
        }

        public void ProcessingError(string message, Exception e)
        {
            using (var db = new ImageArchiveContext())
            {
                string em = (e != null) ? e.Message : null;
                if (em != null && e.InnerException != null && e.InnerException.Message != null)
                {
                    em += " - INNER EXCEPTION -> " + e.InnerException.Message;
                }

                var log = new Log
                {
                    StartDate = DateTime.Now,
                    Type = LogType.Error,
                    Message = message,
                    Exception = em
                };
                db.Logs.Add(log);
                db.SaveChanges();
            }
        }


    }
}
