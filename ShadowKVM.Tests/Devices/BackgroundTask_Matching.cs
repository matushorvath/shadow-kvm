namespace ShadowKVM.Tests;

public class BackgroundTask_MatchingTests : BackgroundTaskFixture
{
    static ActionConfig AC() => new() { Code = new(17), Value = new(98) };
    static Dictionary<nint, List<SetVCPFeatureInvocation>> I_NotCalled() => new();
    static Dictionary<nint, List<SetVCPFeatureInvocation>> I_Called()
        => new() { [0x24680] = [new() { Code = 17, Value = 98 }] };

    static Dictionary<string, TestDatum> GenTestData()
    {
        var testData = new Dictionary<string, TestDatum>();

        // Combinations of description, adapter and serial number that should not match
        var devDescriptions = new[] { "dEsCrIpTiOn" };
        var devAdapters = new[] { null, "aDaPtEr" };
        var devSerials = new[] { null, "sErIaL" };

        var cfgDescriptions = new[] { null, "dEsCrIpTiOn", "dEsCrIpTiOn X" };
        var cfgAdapters = new[] { null, "aDaPtEr", "aDaPtEr X" };
        var cfgSerials = new[] { null, "sErIaL", "sErIaL X" };

        foreach (var devDescription in devDescriptions)
        {
            foreach (var devAdapter in devAdapters)
            {
                foreach (var devSerial in devSerials)
                {
                    foreach (var cfgDescription in cfgDescriptions)
                    {
                        foreach (var cfgAdapter in cfgAdapters)
                        {
                            foreach (var cfgSerial in cfgSerials)
                            {
                                var matches = (cfgDescription == null || cfgDescription == devDescription)
                                    && (cfgAdapter == null || cfgAdapter == devAdapter)
                                    && (cfgSerial == null || cfgSerial == devSerial);

                                var result = matches ? "match" : "no match";
                                var key = $"{result}, description {cfgDescription}, adapter {cfgAdapter}, serial {cfgSerial}";

                                testData.TryAdd(key, new(
                                    new Monitors
                                    {
                                        new()
                                        {
                                            Description = devDescription,
                                            Adapter = devAdapter,
                                            SerialNumber = devSerial,
                                            Handle = H(0x24680)
                                        }
                                    },
                                    [
                                        new()
                                        {
                                            Description = cfgDescription,
                                            Adapter = cfgAdapter,
                                            SerialNumber = cfgSerial,
                                            Attach = AC()
                                        }
                                    ],
                                    IDeviceNotification.Action.Arrival,
                                    matches ? I_Called() : I_NotCalled()
                                ));
                            }
                        }
                    }
                }
            }
        }

        return testData;
    }

    static readonly Dictionary<string, TestDatum> TestData = GenTestData();
    public static TheoryData<string> TestDataKeys => [.. TestData.Keys.AsEnumerable()];

    [Theory, MemberData(nameof(TestDataKeys))]
    public void ProcessOneNotification_Succeeds(string testDataKey)
    {
        TestOneNotification(TestData, testDataKey);
    }
}
