using ImageArchive.DataAccess.Model.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace ImageArchive.DataAccess.Model
{
    public class Log
    {
        public int Id { get; set; }
        public DateTime? StartDate { get; set; }
        //both below saved at the end of batch processing
        public DateTime? EndDate { get; set; }
        public int? FilesProcessed { get; set; }
        //type of log
        public LogType Type { get; set; }
        //file that caused an issue (if required)
        [StringLength(200)]
        public string FileName { get; set; }
        //message updated depending on state
        [StringLength(500)]
        public string Message { get; set; }
        public string Exception { get; set; }
    }
}
