using System;
using System.Text;
using ProgrammingChallenge2.Model;

namespace ProgrammingChallenge2.Codecs.FlorianBader
{
    public class DiffSerializer
    {
        private string _name;
        private Guid? _id;
        private string _statusMessage;
        private bool _selfCheckPassed;
        private bool _serviceModeEnabled;
        private ulong _uptimeInSeconds;
        private double _pressure;
        private double _temperature;
        private double _distance;

        public byte[] Serialize(IotDevice obj)
        {
            var bytes = SerializeType(obj);
            return bytes;
        }

        public IotDevice Deserialize(byte[] data)
        {
            var instance = DeserializeType(data);
            return instance;
        }

        private IotDevice DeserializeType(byte[] data)
        {
            var bitReader = new BitReader(data);

            _name = _name is object ? _name : DeserializeString(bitReader, length: 12, onlyCharacters: true);
            _id = _id is object ? _id : DeserializeGuid(bitReader);
            _statusMessage = bitReader.ReadBit() ? _statusMessage : DeserializeString(bitReader, length: 10, onlyCharacters: true);
            _selfCheckPassed = bitReader.ReadBit() ? _selfCheckPassed : DeserializeBoolean(bitReader);
            _serviceModeEnabled = bitReader.ReadBit() ? _serviceModeEnabled : DeserializeBoolean(bitReader);
            _uptimeInSeconds = _uptimeInSeconds + DeserializeUInt64(bitReader);
            _pressure = bitReader.ReadBit() ? _pressure : DeserializeDouble(bitReader);
            _temperature = bitReader.ReadBit() ? _temperature : DeserializeDouble(bitReader);
            _distance = bitReader.ReadBit() ? _distance : DeserializeDouble(bitReader);

            if (!_statusMessage.Contains(' '))
            {
                _statusMessage = _statusMessage.Substring(0, 3) + " " + _statusMessage.Substring(3, 4) + " " + _statusMessage.Substring(7, 3);
            }

            var instance = new IotDevice(
                _name,
                _id?.ToString("N") ?? string.Empty,
                _statusMessage,
                _selfCheckPassed,
                _serviceModeEnabled,
                _uptimeInSeconds,
                new PhysicalValue(_pressure, "bar"),
                new PhysicalValue(_temperature, "°C"),
                new PhysicalValue(_distance, "m")
            );

            return instance;
        }

        private Guid? DeserializeGuid(BitReader bitReader)
        {
            var value = bitReader.ReadBytes(16);
            return new Guid(value);
        }

        private bool DeserializeBoolean(BitReader bitReader)
        {
            var value = bitReader.ReadBit();
            return value;
        }

        private double DeserializeDouble(BitReader bitReader)
        {
            var bytes = bitReader.ReadBytes(4);
            var value = (double)BitConverter.ToSingle(bytes);
            return value;
        }

        private ulong DeserializeUInt64(BitReader bitReader)
        {
            var bytes = bitReader.ReadBytes(4);
            var value = BitConverter.ToUInt32(bytes);
            return value;
        }

        private string DeserializeString(BitReader bitReader, int length, bool onlyCharacters = false)
        {
            var bytes = bitReader.ReadBytes(length, bitLength: onlyCharacters ? 5 : 6);
            var value = FloEncoding.GetString(bytes, onlyCharacters);
            return value;
        }

        private byte[] SerializeType(IotDevice obj)
        {
            var bitWriter = new BitWriter();

            var id = Guid.Parse(obj.Id);
            var statusMessage = obj.StatusMessage.Replace(" ", string.Empty);

            bitWriter.WriteBit(string.Equals(statusMessage, _statusMessage, StringComparison.Ordinal));
            bitWriter.WriteBit(_selfCheckPassed == obj.SelfCheckPassed);
            bitWriter.WriteBit(_serviceModeEnabled == obj.ServiceModeEnabled);
            bitWriter.WriteBit(IsEqual(_pressure, obj.Pressure.Value));
            bitWriter.WriteBit(IsEqual(_temperature, obj.Temperature.Value));
            bitWriter.WriteBit(IsEqual(_distance, obj.Distance.Value));

            if (!string.Equals(obj.Name, _name, StringComparison.Ordinal))
            {
                SerializeType(bitWriter, obj.Name, onlyCharacters: true);
                _name = obj.Name;
            }

            SerializeType(bitWriter, id);
            _id = id;

            if (!string.Equals(statusMessage, _statusMessage, StringComparison.Ordinal))
            {
                SerializeType(bitWriter, statusMessage, onlyCharacters: true);
                _statusMessage = statusMessage;
            }

            if (_selfCheckPassed != obj.SelfCheckPassed)
            {
                SerializeType(bitWriter, obj.SelfCheckPassed);
                _selfCheckPassed = obj.SelfCheckPassed;
            }

            if (_serviceModeEnabled != obj.ServiceModeEnabled)
            {
                SerializeType(bitWriter, obj.ServiceModeEnabled);
                _serviceModeEnabled = obj.ServiceModeEnabled;
            }

            SerializeType(bitWriter, obj.UptimeInSeconds - _uptimeInSeconds);
            _uptimeInSeconds = obj.UptimeInSeconds;

            if (!IsEqual(_pressure, obj.Pressure.Value))
            {
                SerializeType(bitWriter, obj.Pressure.Value);
                _pressure = obj.Pressure.Value;
            }

            if (!IsEqual(_temperature, obj.Temperature.Value))
            {
                SerializeType(bitWriter, obj.Temperature.Value);
                _temperature = obj.Temperature.Value;
            }

            if (!IsEqual(_distance, obj.Distance.Value))
            {
                SerializeType(bitWriter, obj.Distance.Value);
                _distance = obj.Distance.Value;
            }

            var bytes = bitWriter.ToArray();
            return bytes;
        }

        private bool IsEqual(double lhs, double rhs)
        {
            double difference = Math.Abs(lhs * .000001);
            return Math.Abs(lhs - rhs) <= difference;
        }

        private void SerializeType(BitWriter bitWriter, string value, bool onlyCharacters = false)
        {
            var bytes = FloEncoding.GetBytes(value, onlyCharacters);
            bitWriter.WriteBits(bytes, value.Length * (onlyCharacters ? 5 : 6));
        }

        private void SerializeType(BitWriter bitWriter, bool value)
        {
            bitWriter.WriteBit(value);
        }

        private void SerializeType(BitWriter bitWriter, double value)
        {
            SerializeType(bitWriter, (object)((float)value));
        }

        private void SerializeType(BitWriter bitWriter, ulong value)
        {
            SerializeType(bitWriter, (object)((uint)value));
        }

        private void SerializeType(BitWriter bitWriter, object value)
        {
            var bytes = value switch
            {
                char c => BitConverter.GetBytes(c),
                double d => BitConverter.GetBytes(d),
                short s => BitConverter.GetBytes(s),
                int i => BitConverter.GetBytes(i),
                long l => BitConverter.GetBytes(l),
                float f => BitConverter.GetBytes(f),
                ushort s => BitConverter.GetBytes(s),
                uint i => BitConverter.GetBytes(i),
                ulong l => BitConverter.GetBytes(l),
                Guid g => g.ToByteArray(),
                _ => throw new InvalidOperationException($"Type of {value.GetType()} not supported.")
            };

            bitWriter.WriteBytes(bytes);
        }
    }
}