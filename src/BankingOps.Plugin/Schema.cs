
namespace BankingOps.Plugin
{
    /// <summary>
    /// Central place to rename entity/column logical names to match YOUR banking schema.
    /// Update these constants and re-register steps accordingly.
    /// </summary>
    public static class Schema
    {
        // Transaction table
        public const string TransactionEntity = "bkg_transaction";             // e.g., new_banktransaction
        public const string TransactionAmount = "bkg_amount";                  // Money
        public const string TransactionCurrency = "bkg_currency";              // String (ISO)
        public const string TransactionCustomer = "bkg_customerid";            // Lookup(Account/Contact)
        public const string TransactionFraudScore = "bkg_fraudscore";          // Decimal
        public const string TransactionIsFraud = "bkg_isfraudrisk";            // Boolean

        // Customer table + fields used by plugins/APIs
        public const string CustomerAccountEntity = "account";                 // Or bkg_customer
        public const string CustomerKycField = "bkg_kycstatus";                // OptionSet: 100000000 = Passed
        public const string CustomerCreditLimitField = "bkg_creditlimit";      // Money
        public const string CustomerCurrentExposureField = "bkg_currentexposure"; // Money
        public const string CustomerCreditScoreField = "bkg_creditscore";      // Integer
        public const string CustomerMonthlyIncomeField = "bkg_monthlyincome";  // Money
    }
}
