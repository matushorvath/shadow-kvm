using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace ShadowKVM;

public class TriggerDevice
{
    public TriggerDeviceClass Class { get; set; } = new(TriggerDeviceType.Keyboard);
    public int? VendorId { get; set; }
    public int? ProductId { get; set; }

    // Format of the structure as it was loaded; null if not loaded
    public int? LoadedVersion { get; set; }
}

public class TriggerDeviceTypeConverter() : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(TriggerDevice);

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var startMark = parser.Current!.Start;
        var endMark = parser.Current!.End;

        if (parser.TryConsume<MappingStart>(out _))
        {
            // Config version 2 has a mapping here
            var triggerDevice = new TriggerDevice { LoadedVersion = 2 };
            while (!parser.TryConsume<MappingEnd>(out _))
            {
                ConsumePropertyVersion2(parser, triggerDevice, rootDeserializer);
            }

            if (triggerDevice.Class == null)
            {
                throw new YamlException(startMark, endMark, $"Missing trigger device class");
            }

            return triggerDevice;
        }
        else
        {
            // Config version 1 has just a scalar with TriggerDeviceClass
            var triggerDeviceClass = (TriggerDeviceClass?)rootDeserializer(typeof(TriggerDeviceClass));
            if (triggerDeviceClass == null)
            {
                throw new YamlException(startMark, endMark, $"Null trigger device class");
            }

            return new TriggerDevice { LoadedVersion = 1, Class = triggerDeviceClass };
        }
    }

    void ConsumePropertyVersion2(IParser parser, TriggerDevice triggerDevice, ObjectDeserializer rootDeserializer)
    {
        var startMark = parser.Current!.Start;
        var endMark = parser.Current!.End;

        var key = parser.Consume<Scalar>();

        if (key.Value == "class")
        {
            var triggerDeviceClass = (TriggerDeviceClass?)rootDeserializer(typeof(TriggerDeviceClass));
            if (triggerDeviceClass == null)
            {
                throw new YamlException(startMark, endMark, $"Null trigger device class");
            }

            triggerDevice.Class = triggerDeviceClass;
        }
        else if (key.Value == "vendor-id")
        {
            triggerDevice.VendorId = int.Parse(parser.Consume<Scalar>().Value, NumberStyles.HexNumber);
        }
        else if (key.Value == "product-id")
        {
            triggerDevice.ProductId = int.Parse(parser.Consume<Scalar>().Value, NumberStyles.HexNumber);
        }
        else
        {
            throw new Exception("Invalid property name");
        }
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        throw new NotImplementedException("Serialization of TriggerDevice to Yaml is not implemented");
    }
}
