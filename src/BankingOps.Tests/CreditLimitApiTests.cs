
using Xunit;
using FakeXrmEasy;
using Microsoft.Xrm.Sdk;
using System;

namespace BankingOps.Tests
{
    public class CreditLimitApiTests
    {
        [Fact]
        public void CheckCreditLimit_ReturnsWithinLimit()
        {
            var ctx = new XrmFakedContext();
            var acct = new Entity(BankingOps.Plugin.Schema.CustomerAccountEntity) { Id = Guid.NewGuid() };
            acct[BankingOps.Plugin.Schema.CustomerCreditLimitField] = new Money(10000m);
            acct[BankingOps.Plugin.Schema.CustomerCurrentExposureField] = new Money(2000m);
            ctx.Initialize(new[] { acct });

            var plugin = new BankingOps.Plugin.CheckCreditLimitCustomApi(null, null);
            var input = new ParameterCollection
            {
                {"CustomerId", acct.Id },
                {"RequestedAmount", 3000m }
            };
            var output = ctx.ExecutePluginWithOutputParameters(plugin, input);
            Assert.True((bool)output["IsWithinLimit"]);
        }
    }
}
