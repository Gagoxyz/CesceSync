using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CesceSync.Models;

public class MovimientosResponse
{
    public List<MovimientoCesce> items { get; set; }
    public long nextEndorsementNo { get; set; }
}
