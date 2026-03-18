using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CesceSync.Config;

public class LoggingFilesOptions
{
    public string BasePath { get; set; } = string.Empty;
    public int RetentionDays { get; set; } = 7;
    public string FilePrefix { get; set; } = "CesceSync";
}
