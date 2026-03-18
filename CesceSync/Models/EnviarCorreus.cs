using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CesceSync.Models;

public class EnviarCorreusOptions
{
    public string ExePath { get; set; } = "";
    public string SqlServer { get; set; } = "";
    public string Database { get; set; } = "";
    public string UserDB { get; set; } = "";
    public string PasswordDB { get; set; } = "";
    public int TimeoutSeconds { get; set; } = 120;
    public string Servidor { get; set; } = "";
    public int Puerto { get; set; } = 587;
    public bool UseSSL { get; set; } = true;
    public string MailOrigen { get; set; } = "";
    public string Usuario { get; set; } = "";
    public string Pass { get; set; } = "";
    public string DefaultPara { get; set; } = "";
    public string DefaultAsunto { get; set; } = "";
    public short DefaultQuien { get; set; } = 999;
}

public class EnviarCorreusResult
{
    public int ExitCode { get; set; }
    public string StdOut { get; set; } = "";
    public string StdErr { get; set; } = "";
    public bool TimedOut { get; set; }
}
