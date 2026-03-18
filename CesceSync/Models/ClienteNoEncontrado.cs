using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CesceSync.Models;

public class ClienteNoEncontrado
{
    public string NIF { get; set; }
    public string Poliza { get; set; }
    public decimal RiesgoMaximo { get; set; }
    public DateTime FechaRiesgo { get; set; }
}
