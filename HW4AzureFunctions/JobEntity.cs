using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HW4AzureFunctions
{
    public class JobEntity : TableEntity
    {
        public string ConversionType { get; set; }

        public string Status { get; set; }

        public string ResultDetailsMessage { get; set; }
    }
}