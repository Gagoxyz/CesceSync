using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CesceSync.Config;

public class CesceApiConfig
{
    public string BaseUrl { get; set; }
    public string TokenUrl { get; set; }
    public string MovimientosUrl { get; set; }
    public string VentasUrl { get; set; }
    public string Scope { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string LanguageCode { get; set; } = "ES";
    public bool UseGrantedAmountForRisk { get; set; } = false;
}
