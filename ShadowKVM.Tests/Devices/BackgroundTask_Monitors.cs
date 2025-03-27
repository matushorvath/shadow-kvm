using Moq;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using Windows.Win32.Foundation;

namespace ShadowKVM.Tests;

public class BackgroundTask_MonitorsTests : BackgroundTaskFixture
{
    static SafePhysicalMonitorHandle H(nuint value) => new SafePhysicalMonitorHandle(null!, (HANDLE)value, false);

    record TestDatum(
        Monitors monitorDevices,
        MonitorConfig[]? monitorConfigs,
        IDeviceNotification.Action action,
        Dictionary<nint, List<SetVCPFeatureInvocation>> invocations);

    static Dictionary<string, TestDatum> TestData => new()
    {
        ["no monitors"] = new(
            new Monitors(),
            [],
            IDeviceNotification.Action.Arrival,
            new Dictionary<nint, List<SetVCPFeatureInvocation>>()
        ),
        ["null configured monitors"] = new(
            new Monitors
            {
                new() { Description = "dEsCrIpTiOn 1", Handle = H(0x34567u) },
                new() { Description = "dEsCrIpTiOn 2", Handle = H(0x45689u) }
            },
            null,
            IDeviceNotification.Action.Removal,
            new Dictionary<nint, List<SetVCPFeatureInvocation>>()
        ),
        ["zero configured monitors"] = new(
            new Monitors
            {
                new() { Description = "dEsCrIpTiOn 1", Handle = H(0x34567u) },
                new() { Description = "dEsCrIpTiOn 2", Handle = H(0x45689u) }
            },
            [],
            IDeviceNotification.Action.Removal,
            new Dictionary<nint, List<SetVCPFeatureInvocation>>()
        ),
        ["no monitor devices"] = new(
            new Monitors(),
            [
                new()
                {
                    Description = "dEsCrIpTiOn 1",
                    Attach = new () { Code = new(42), Value = new (76) }
                },
                new()
                {
                    Description = "dEsCrIpTiOn 2",
                    Attach = new () { Code = new(43), Value = new (75) }
                }
            ],
            IDeviceNotification.Action.Arrival,
            new Dictionary<nint, List<SetVCPFeatureInvocation>>()
        ),
        ["attach with missing attach config"] = new(
            new Monitors
            {
                new() { Description = "dEsCrIpTiOn 1", Handle = H(0x56789u) }
            },
            [
                new()
                {
                    Description = "dEsCrIpTiOn 1",
                    Detach = new () { Code = new(17), Value = new (98) }
                }
            ],
            IDeviceNotification.Action.Arrival,
            new Dictionary<nint, List<SetVCPFeatureInvocation>>()
        ),
        ["detach with missing attach config"] = new(
            new Monitors
            {
                new() { Description = "dEsCrIpTiOn 1", Handle = H(0x56789u) }
            },
            [
                new()
                {
                    Description = "dEsCrIpTiOn 1",
                    Attach = new () { Code = new(17), Value = new (98) }
                }
            ],
            IDeviceNotification.Action.Removal,
            new Dictionary<nint, List<SetVCPFeatureInvocation>>()
        ),
        ["attach one monitor"] = new(
            new Monitors
            {
                new() { Description = "dEsCrIpTiOn 1", Handle = H(0x12345u) }
            },
            [
                new MonitorConfig
                {
                    Description = "dEsCrIpTiOn 1",
                    Attach = new () { Code = new(17), Value = new (98) }
                }
            ],
            IDeviceNotification.Action.Arrival,
            new Dictionary<nint, List<SetVCPFeatureInvocation>>
            {
                [0x12345] = [new() { Code = 17, Value = 98 }]
            }
        ),
        ["detach one monitor"] = new(
            new Monitors
            {
                new() { Description = "dEsCrIpTiOn 1", Handle = H(0x23456u) }
            },
            [
                new()
                {
                    Description = "dEsCrIpTiOn 1",
                    Detach = new () { Code = new(42), Value = new (76) }
                }
            ],
            IDeviceNotification.Action.Removal,
            new Dictionary<nint, List<SetVCPFeatureInvocation>>
            {
                [0x23456] = [new() { Code = 42, Value = 76 }]
            }
        ),
        ["one monitor matches multiple configs"] = new(
            new Monitors
            {
                new() { Description = "dEsCrIpTiOn 1", Adapter = "aDaPtEr 1", Handle = H(0x23456u) }
            },
            [
                new()
                {
                    Description = "dEsCrIpTiOn 1",
                    Detach = new () { Code = new(42), Value = new (76) }
                },
                new()
                {
                    Adapter = "aDaPtEr 1",
                    Detach = new () { Code = new(43), Value = new (75) }
                }
            ],
            IDeviceNotification.Action.Removal,
            new Dictionary<nint, List<SetVCPFeatureInvocation>>
            {
                [0x23456] =
                [
                    new() { Code = 42, Value = 76 },
                    new() { Code = 43, Value = 75 }
                ]
            }
        ),
        ["multiple monitors match one config"] = new(
            new Monitors
            {
                new() { Description = "dEsCrIpTiOn 1", Adapter = "aDaPtEr", Handle = H(0x23456u) },
                new() { Description = "dEsCrIpTiOn 2", Adapter = "aDaPtEr", Handle = H(0x34564u) }
            },
            [
                new()
                {
                    Adapter = "aDaPtEr",
                    Attach = new () { Code = new(17), Value = new (98) }
                },
            ],
            IDeviceNotification.Action.Arrival,
            new Dictionary<nint, List<SetVCPFeatureInvocation>>
            {
                [0x23456] = [new() { Code = 17, Value = 98 }],
                [0x34564] = [new() { Code = 17, Value = 98 }],
            }
        ),
        ["attach multiple monitors"] = new(
            new Monitors
            {
                new() { Description = "dEsCrIpTiOn 1", Handle = H(0x12345u) },
                new() { Description = "dEsCrIpTiOn 2", Adapter="aDaPtEr 2", Handle = H(0x23456u) },
                new() { Description = "dEsCrIpTiOn UnUsEd 3", Handle = H(0x34567u) },
                new() { Description = "dEsCrIpTiOn 4", SerialNumber="sErIaL 4", Handle = H(0x45678u) },
            },
            [
                new MonitorConfig
                {
                    Description = "dEsCrIpTiOn 1",
                    Attach = new () { Code = new(17), Value = new (98) }
                },
                new MonitorConfig
                {
                    Description = "dEsCrIpTiOn 2",
                    Adapter = "aDaPtEr 2",
                    Attach = new () { Code = new(18), Value = new (97) },
                    Detach = new () { Code = new(43), Value = new (75) }
                },
                new MonitorConfig
                {
                    SerialNumber = "sErIaL 4",
                    Attach = new () { Code = new(19), Value = new (96) }
                },
                new MonitorConfig
                {
                    Description = "dEsCrIpTiOn nOnExIsTeNt",
                    Attach = new () { Code = new(20), Value = new (95) }
                }
            ],
            IDeviceNotification.Action.Arrival,
            new Dictionary<nint, List<SetVCPFeatureInvocation>>
            {
                [0x12345] = [new() { Code = 17, Value = 98 }],
                [0x23456] = [new() { Code = 18, Value = 97 }],
                [0x45678] = [new() { Code = 19, Value = 96 }]
            }
        ),
    };

    public static TheoryData<string> TestDataKeys => [.. TestData.Keys.AsEnumerable()];

    [Theory, MemberData(nameof(TestDataKeys))]
    public void ProcessOneNotification_Succeeds(string testDataKey)
    {
        var (monitorDevices, monitorConfigs, action, invocations) = TestData[testDataKey];

        // The mock notification will pass data to this channel
        var channel = Channel.CreateUnbounded<IDeviceNotification.Action>();
        var notificationMock = SetupNotification(channel);

        var finishedEvent = new AutoResetEvent(false);
        SetupForProcessOneNotification(monitorDevices, invocations, finishedEvent);

        var config = new Config
        {
            TriggerDevice = new() { Raw = _testGuid },
            Monitors = monitorConfigs?.ToList()
        };

        using (var backgroundTask = new BackgroundTask(_deviceNotificationServiceMock.Object,
            _monitorServiceMock.Object, _windowsAPIMock.Object, _loggerMock.Object))
        {
            backgroundTask.Restart(config);

            // Task is now running, send it an action
            channel.Writer.TryWrite(action);

            // Wait for the background task to make all calls to SetVCPFeature
            Assert.True(finishedEvent.WaitOne(TimeSpan.FromSeconds(5)));
        }

        // Check no other SetVCPFeature calls were made
        _windowsAPIMock
            .Verify(
                m => m.SetVCPFeature(It.IsAny<SafeHandle>(), It.IsAny<byte>(), It.IsAny<uint>()),
                Times.Exactly(invocations.Sum(i => i.Value.Count)));

        _monitorServiceMock.Verify();
        _deviceNotificationServiceMock.Verify();
        notificationMock.Verify();
    }
}
