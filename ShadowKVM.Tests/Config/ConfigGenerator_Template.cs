using System.Text.RegularExpressions;
using Moq;

namespace ShadowKVM.Tests;

public class ConfigGenerator_TemplateTest
{
    internal Mock<IMonitorService> _monitorServiceMock = new();
    internal Mock<IMonitorInputService> _monitorInputServiceMock = new();

    static SafePhysicalMonitorHandle NH = SafePhysicalMonitorHandle.Null; // short name for null handle

    // TODO
    // all codes, all values
    // unsupported when inputs = null
    // formatted SelectedInput when enum, when raw, when null
    // formatted UnselectedInputStringAndComment when SelectedInput null and unselected = []
    //   when Selected not null and unselected = []
    //   when Selected not null and unselected = exactly one
    //   when Selected not null and unselected = more than one
    // the whole text can be loaded and has trigger-device, monitors, log-level, version
    // monitors:
    //   none, one, multiple
    //   without adapter and serial, with just adapter, just serial, both

    public class X
    {
        public required string Prop { get; set; }
        public override string ToString()
        {
            return $"X:{Prop}";
        }
    }

    public static IEnumerable<object[]> TestData =>
    [
        [
            //new X { Prop = "123" },
            "aaa"
        ],
         [
             //new X { Prop = "123" },

            "bb"
        ],
        [
           //new X { Prop = "123" },
            
            "c"
        ]
    ];

    [Theory]
    [MemberData(nameof(TestData))]
    public void Generate_CorrectText(string expectedFragment)
    {
        // SetupForTemplate(monitors, monitorInputs);

        // var generator = new ConfigGenerator(_monitorServiceMock.Object, _monitorInputServiceMock.Object);
        // var text = generator.Generate();

        // Assert.StartsWith("# ShadowKVM automatically switches", text);
        // Assert.EndsWith("version: 1\r\n", text);
        // // TODO make sure it's valid yaml, or even try to load it with ConfigService

        Assert.Contains(expectedFragment, "bbaaacc");
    }

    void SetupForTemplate(Monitors monitors, IList<MonitorInputs> monitorInputs)
    {
        _monitorServiceMock
            .Setup(m => m.LoadMonitors())
            .Returns(monitors);

        var loadMonitorInputsInvocation = 0;
        MonitorInputs? inputs;

        _monitorInputServiceMock
            .Setup(m => m.TryLoadMonitorInputs(It.IsAny<Monitor>(), out inputs))
            .Returns(
                (Monitor monitor, out MonitorInputs? inputs) =>
                {
                    inputs = monitorInputs[loadMonitorInputsInvocation++];
                    return true;
                }
            );
    }
}
