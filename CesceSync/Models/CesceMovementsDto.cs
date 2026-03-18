using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CesceSync.Models;

// Envoltura completa de la respuesta
public class MovementsEnvelope
{
    public MovementsError error { get; set; }
    public MovementsClient client { get; set; }
    public List<DebtorDto> debtor { get; set; }
}

public class MovementsError
{
    public string errorCode { get; set; }
    public string errorDescription { get; set; }
}

public class MovementsClient
{
    public string contractNo { get; set; }
    public string nextEndorsementNo { get; set; } // string en JSON
}

// Item de deudor (tal como llega del API)
public class DebtorDto
{
    public string countryCode { get; set; }
    public string taxCode { get; set; }             // NIF/NIE/Extranjero
    public string endorsementNo { get; set; }       // Póliza (string)
    public string customerReference { get; set; }
    public string companyName { get; set; }
    public string address { get; set; }
    public string city { get; set; }
    public string state { get; set; }
    public string postalCode { get; set; }
    public string phoneNo { get; set; }
    public string email { get; set; }

    public string creditLimitRequested { get; set; } // "100000,00"
    public string paymentMethodRequestedCode { get; set; }
    public string paymentTermsRequested { get; set; }
    public string currencyCode { get; set; }

    public string creditLimitGranted { get; set; } // "25000,00" o "0,00"
    public string commercialRiskCoverPct { get; set; }
    public string politicalRiskCoverPct { get; set; }
    public string paymentMethodGrantedCode { get; set; }
    public string paymentTermsGranted { get; set; }

    public string commercialRiskGroupCode { get; set; }
    public string politicalRiskGroupCode { get; set; }
    public string classificationDecisionCode { get; set; }

    public string statusCode { get; set; }          // "66", "2", "10", ...
    public string cesceCode { get; set; }

    public string requestEntryDate { get; set; }    // "yyyyMMdd" o "0"
    public string effectiveDate { get; set; }       // "yyyyMMdd" o "0"
    public string cancellationDate { get; set; }    // "yyyyMMdd" o "0"
    public string validityDate { get; set; }        // "yyyyMMdd" o "0"
    public string classificationDate { get; set; }  // "yyyyMMdd" o "0"
    public string validityReasonCode { get; set; }
    public string commentsInd { get; set; }
}