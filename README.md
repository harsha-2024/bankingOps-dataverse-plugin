
# BankingOps Plugin – CLI-first Template

This repo is **parameterized** so you can quickly rename entities/fields to match your banking schema (see `Schema.cs`), deploy via **Power Platform CLI**, and run in CI with an **Azure DevOps pipeline**.

## Rename entities & fields
Open `src/BankingOps.Plugin/Schema.cs` and update the logical names to your actual Dataverse schema. Rebuild and re-register steps with those names.

## Included plug-ins / APIs
- `TransactionValidationPlugin` – Pre-Operation validation on your transaction table
- `FraudScoreEnrichmentPlugin` – Post-Operation async enrichment via external Fraud API
- `GetFxQuoteCustomApi` – Returns FX rate
- `CheckCreditLimitCustomApi` – Checks available credit for a customer
- `EvaluateLoanEligibilityCustomApi` – Simple loan eligibility decisioning using Credit Score & DTI with env-based policy

## CLI-first deployment
See `scripts/pac-commands.md` for step-by-step **pac** commands to create a solution, add the project, pack, and import. Step registration guidance is in `scripts/register-steps.md`.

## Unit tests
An `xUnit` project with `FakeXrmEasy` shows how to test Custom APIs and plug-ins without a live org.

## Notes
- Target framework: **.NET Framework 4.6.2** (Dataverse plug-ins). Build with VS 2019+ or `dotnet` SDK.
- Keep secrets in **Secure Configuration** at the step level or use managed identity when available.
