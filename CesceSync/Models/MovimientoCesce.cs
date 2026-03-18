using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CesceSync.Models;

public class MovimientoCesce
{
    public string contractNo { get; set; }
    public string endorsementNo { get; set; }        // Poliza
    public string statusCode { get; set; }           // "66","2","10",...
    public decimal creditLimitRequested { get; set; }
    public decimal? creditLimitGranted { get; set; } // por si un día lo usas
    public DateTime? effectiveDate { get; set; }     // null si "0"
    public DateTime? validityDate { get; set; }      // null si "0"
    public DateTime? cancellationDate { get; set; }  // null si "0"
    public string taxCode { get; set; }              // NIF/NIE/Extranjero
    public string currencyCode { get; set; } = "EUR";
}
