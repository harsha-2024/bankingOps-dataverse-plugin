
# BankingOps.Plugin (Microsoft Dataverse / Dynamics 365)

A production-grade sample plugin package for complex banking scenarios. It demonstrates:

- Pre-operation validation with **pre-images**
- Post-operation asynchronous enrichment via **HTTP** with retry
- **Idempotency** to avoid duplicate processing
- **Environment Variables** + **Secure/Unsecure** configuration
- **Custom API** implementation for FX quotes
- Extensive **tracing** for Plugin Trace Log

> Target framework: **.NET Framework 4.6.2** (per Dataverse plugin requirement).

## Projects
- `BankingOps.Plugin` – class library containing multiple plugin classes:
  - `TransactionValidationPlugin` – Pre-Operation validation on `new_banktransaction` (Create/Update).
  - `FraudScoreEnrichmentPlugin` – Post-Operation async enrichment querying an external Fraud API.
  - `GetFxQuoteCustomApi` – Custom API handler returning an FX quote.

## Build
1. Open `BankingOps.Plugin.csproj` in Visual Studio 2019+.
2. Restore NuGet packages.
3. Build in **Release** mode to produce `BankingOps.Plugin.dll`.

## Register
You can register with **Plug-in Registration Tool (PRT)** or **Power Platform Tools**.

### Using PRT
1. Launch PRT and connect to your environment.
2. **Register New Assembly** → select the built DLL.
3. Add **Steps**:
   - **TransactionValidationPlugin**
     - Message: `Create`, Primary Entity: `new_banktransaction`, Stage: `Pre-Operation`
     - Message: `Update`, Primary Entity: `new_banktransaction`, Stage: `Pre-Operation`
     - Pre-Image: `PreImageTxn` with attributes: `new_amount`, `new_currency`, `new_customerid`
     - Unsecure Config (optional): `USD,EUR,GBP,INR` (comma-separated allowed currencies)
   - **FraudScoreEnrichmentPlugin**
     - Message: `Create`, Primary Entity: `new_banktransaction`, Stage: `Post-Operation`, Async
     - Message: `Update`, Primary Entity: `new_banktransaction`, Stage: `Post-Operation`, Async
     - Secure Config (optional): API key/token
     - Environment Variable (recommended): `pp_FraudApiUrl` → URL of the fraud scoring API
   - **GetFxQuoteCustomApi**
     - Register as handler for a **Custom API** named `new_GetFxQuote` with input params `Base` (String), `Counter` (String) and output `Rate` (Decimal).

> **Tip**: Enable **Plugin Trace Log** (Settings → Administration → System Settings → Customization) to view traces.

## Dataverse Configuration
- Custom table required for idempotency:
  - Logical name: `new_plugincache`
  - Columns: `new_name` (Single Line of Text, Primary/Alternate Key), `new_expirationon` (DateTime, optional)
- Environment Variables:
  - `pp_AllowedCurrencies` – optional allowed currencies list
  - `pp_FraudApiUrl` – fraud service base URL
  - Optional static rates like `pp_StaticFx_USD_EUR`

## Security & External Calls
- For sensitive secrets, prefer **Secure Configuration** on the step.
- For enterprise-grade setups, consider **Managed Identity** or **Azure Service Bus** decoupling.

## Notes
- All schema names like `new_banktransaction` are examples—adjust to your solution.
- The `FraudScoreEnrichmentPlugin` uses `HttpClient` and a simple JSON parse; adapt to your API’s shape.

## License
MIT
