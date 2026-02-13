
# Step registration checklist

1. Open **Plug-in Registration Tool** (PRT), connect to your environment.
2. Verify the assembly **BankingOps.Plugin** is present (if you imported via solution). If not, **Register New Assembly**.
3. Add Steps:
   - **TransactionValidationPlugin**
     - Message: `Create` + `Update`
     - Primary Entity: value of `Schema.TransactionEntity` (default `bkg_transaction`)
     - Stage: `Pre-Operation`
     - **Pre-Image** name: `PreImageTxn` with attributes: `Schema.TransactionAmount`, `Schema.TransactionCurrency`, `Schema.TransactionCustomer`
     - Unsecure Config (optional CSV): allowed currency list (e.g., `USD,EUR,GBP,INR`)
   - **FraudScoreEnrichmentPlugin**
     - Message: `Create` + `Update`
     - Primary Entity: `Schema.TransactionEntity`
     - Stage: `Post-Operation`
     - **Asynchronous** execution
     - Secure Config (optional): fraud API token; Env Var: `pp_FraudApiUrl`
   - **GetFxQuoteCustomApi**
     - Handler for Custom API `new_GetFxQuote` with inputs `Base`, `Counter`, output `Rate`
   - **CheckCreditLimitCustomApi**
     - Handler for Custom API `new_CheckCreditLimit` with inputs `CustomerId` (Guid), `RequestedAmount` (Decimal); outputs `IsWithinLimit` (Boolean), `AvailableLimit` (Decimal)
   - **EvaluateLoanEligibilityCustomApi**
     - Handler for Custom API `new_EvaluateLoanEligibility` with inputs `CustomerId` (Guid), `ProductCode` (String), `RequestedAmount` (Decimal); outputs `Eligible` (Boolean), `Reasons` (String)

> Don’t forget to enable **Plugin Trace Log** for troubleshooting.
