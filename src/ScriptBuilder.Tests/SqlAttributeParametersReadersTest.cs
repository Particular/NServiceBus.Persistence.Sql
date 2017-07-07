using System.Collections.Generic;
using NUnit.Framework;
using ObjectApproval;

[TestFixture]
public class SqlAttributeParametersReadersTest
{

    [Test]
    public void Defaults()
    {
        var result = SettingsAttributeReader.Read(
            new CustomAttributeMock(
                new Dictionary<string, object>
                {
                    {
                        //At least one is required
                        "MsSqlServerScripts", true
                    }
                }));
        ObjectApprover.VerifyWithJson(result);
    }

    [Test]
    public void NonDefaults()
    {
        var result = SettingsAttributeReader.Read(
            new CustomAttributeMock(
                new Dictionary<string, object>
                {
                    {
                        "ScriptPromotionPath", @"D:\scripts"
                    },
                    {
                        "MsSqlServerScripts", true
                    },
                    {
                        "MySqlScripts", true
                    },
                    {
                        "OracleScripts", true
                    },
                    {
                        "ProduceSagaScripts", false
                    },
                    {
                        "ProduceTimeoutScripts", false
                    },
                    {
                        "ProduceSubscriptionScripts", false
                    },
                    {
                        "ProduceOutboxScripts", false
                    }
                }));
        ObjectApprover.VerifyWithJson(result);
    }


}