using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CesceSync.Models;

public class TokenRequest
{
    public string grant_type { get; set; } = "password";
    public string client_id { get; set; }
    public string client_secret { get; set; }
    public string scope { get; set; }
    public string username { get; set; }
    public string password { get; set; }
}
