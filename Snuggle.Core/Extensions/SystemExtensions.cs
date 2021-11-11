using System;
using System.Collections.Generic;

namespace Snuggle.Core.Extensions {
    public static class SystemExtensions {
        public static string ToFlagString(this Enum @enum) {
            var value = Convert.ToUInt64(@enum);
            if (value == 0) {
                return "None";
            }

            var enumType = @enum.GetType();
            var type = Enum.GetUnderlyingType(enumType);
            byte bits = type.Name switch {
                "Byte" => 8,
                "SByte" => 8,
                "Int16" => 16,
                "UInt16" => 16,
                "Int32" => 32,
                "UInt32" => 32,
                "Int64" => 64,
                "UInt64" => 64,
                _ => 32,
            };
            var values = new List<string>(bits);
            for (var i = 0; i < bits; ++i) {
                var bitValue = 1UL << i;
                if ((value & bitValue) != 0) {
                    var actualValue = Convert.ChangeType(bitValue, type);
                    values.Add(Enum.IsDefined(enumType, actualValue) ? Enum.Format(enumType, actualValue, "G") : "0x" + bitValue.ToString("X"));
                }
            }

            return string.Join(", ", values);
        }
    }
}
