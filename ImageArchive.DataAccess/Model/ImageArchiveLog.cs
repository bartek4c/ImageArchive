using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageArchive.DataAccess.Model
{
    public class ArchivedImage
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        [StringLength(50)]
        public string OriginalName { get; set; }
        [StringLength(10)]
        public string Extension { get; set; }
        //new file details
        [StringLength(50)]
        public string ImageId { get; set; }
        public int? Year { get; set; }
        public int? Month { get; set; }
    }
}
