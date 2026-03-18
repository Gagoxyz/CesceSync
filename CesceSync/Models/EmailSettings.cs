using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CesceSync.Models
{
    public class EmailSettings
    {
        public string Host { get; set; } = "";
        public int Port { get; set; }
        public bool UseSSL { get; set; }
        public bool UseStartTls { get; set; }
        public string From { get; set; } = "";
        public string User { get; set; } = "";
        public string Password { get; set; } = "";
        public string To { get; set; } = "";
    }

}
