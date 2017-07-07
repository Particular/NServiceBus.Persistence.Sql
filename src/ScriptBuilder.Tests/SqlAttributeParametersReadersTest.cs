using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ObjectApproval;

[TestFixture]
public class SqlAttributeParametersReadersTest
{

    [Test]
    public void Variant()
    {
        var result = SettingsAttributeReader.Read(
            new CustomAttributeMock(
                new Dictionary<string, object>
                {
                    {
                        "MsSqlServerScripts", true
                    },
                    {
                        "OracleScripts", true
                    }
                }));
        ObjectApprover.VerifyWithJson(result.BuildVariants.ToList());
    }

    [Test]
    public void ScriptPromotionPath()
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
                    }
                }));
        ObjectApprover.VerifyWithJson(result.ScriptPromotionPath);
    }

}