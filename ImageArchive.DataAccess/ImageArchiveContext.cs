using ImageArchive.DataAccess.Model;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageArchive.DataAccess
{
    public class ImageArchiveContext : DbContext
    {
        public ImageArchiveContext() : base("name=ImageArchiveContext")
        {

        }

        public DbSet<ArchivedImage> ArchivedImages { get; set; }
        public DbSet<Log> Logs { get; set; }
    }
}
