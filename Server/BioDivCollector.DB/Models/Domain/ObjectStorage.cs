using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BioDivCollector.DB.Models.Domain
{
    public class ObjectStorage
    {
        [Key]
        public Guid ObjectStorageId { get; set; }

        public string OriginalFileName { get; set; }

        public string SavedFileName { get; set; }

        public string SavedFilePath { get; set; }

        /// <summary>
        /// meta data (e.g. exif)
        /// </summary>
        public string Metadata { get; set; }


        public void ResetObjectStorage()
        {
            this.SavedFilePath = null;
            this.SavedFileName = null;
            this.Metadata = null;
            this.OriginalFileName = null;
        }

        public bool IsNotSet => 
            string.IsNullOrWhiteSpace(this.SavedFileName) && string.IsNullOrWhiteSpace(this.SavedFilePath) && string.IsNullOrWhiteSpace(this.OriginalFileName);
    }


}
