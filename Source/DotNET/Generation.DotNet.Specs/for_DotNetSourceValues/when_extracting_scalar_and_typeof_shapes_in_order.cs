// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_extracting_scalar_and_typeof_shapes_in_order : given.a_value_snapshot
{
    const string Source = """
        namespace Values;

        public enum Color { Red = 1, Crimson = 1, Blue = 2, Green = 3 }

        public enum Plain { A, B }

        public readonly struct Wrapped
        {
            public static implicit operator Wrapped(int value) => default;
        }

        public static class Usage
        {
            const int Known = 5;
            static bool Flag = true;
            static int Runtime() => 1;

            public static int F01 = 42;
            public static string F02 = "text";
            public static string? F03 = null;
            public static int F04 = (42);
            public static int F05 = Known;
            public static long F06 = Known;
            public static System.Type F07 = typeof(string);
            public static System.Type F08 = (typeof(int));
            public static System.Type F09 = typeof(System.Collections.Generic.List<>);
            public static System.Type F10 = typeof(Missing);
            public static int F11 = Flag ? 1 : 2;
            public static int F12 = (int)(Flag ? 1 : 2);
            public static object? F13 = Flag ? null : "a";
            public static int F14 = Runtime();
            public static int F15 = Known + 1;
            public static Color F16 = Color.Red;
            public static Color F17 = Color.Crimson;
            public static Color F18 = (Color)1;
            public static Color F19 = (Color)7;
            public static Color? F20 = Color.Blue;
            public static Color? F21 = null;
            public static int F22 = Missing;
            public static int F23 = Missing.Value;
            public static dynamic F24 = 1;
            public static int F25 = checked(Known + 1);
            public static string F26 = $"x{Known}";
            public static string F27 = "a" + "b";
            public static Plain F28 = Plain.B;
            public static char F29 = 'c';
            public static decimal F30 = 1.5m;
            public static object F31 = new object();
            public static int F32 = default;
            public static int F33 = int.MaxValue;
            public static string F34 = nameof(Known);
            public static Wrapped F35 = 5;
            public static int F36 = 1 + "a";
            public static string? F37 = F03 ?? "x";
            public static bool F38 = Flag && true;
            public static object F39 = typeof(string);
            public static System.Type F40 = (System.Type)typeof(string);
            public static System.Type F41 = (typeof(System.Collections.Generic.List<>));
            public static int F42 = (int)1.5;
            public static int F43 = Flag switch { true => 1, false => 2 };
            public static int F44 = (Flag ? 1 : 2) + 1;
            public static int F45 = Known!;
            public static int? F46 = Flag ? 1 : null;
            public static System.Type F47 = typeof(dynamic[]);
            public static System.Type F48 = typeof(Plain);
            public static int F49 = Usage.Known;
            public static byte F50 = 300;
        }
        """;

    string[] _snapshot = null!;

    void Because() => _snapshot = SnapshotOf("/workspace/Scalars.cs", Source);

    [Fact]
    void should_snapshot_every_shape_in_declaration_order() => _snapshot.ShouldEqual(
    [
        "Known: known const(5:int)",
        "Flag: known const(True:bool)",
        "F01: known const(42:int)",
        "F02: known const(text:string)",
        "F03: known const(:string?)",
        "F04: unknown [Unsupported 20:28-20:32 'The authored value conversion is not semantically valid']",
        "F05: known const(5:int)",
        "F06: known const(5:long)",
        "F07: known typeof(string)",
        "F08: unknown [Unsupported 24:36-24:49 'The authored typeof value conversion is not semantically valid']",
        "F09: unknown [Unbound 25:36-25:77 'The typeof operand has no exact error-free binding']",
        "F10: unknown [Unbound 26:36-26:51 'The typeof operand has no exact error-free binding']",
        "F11: unknown [Conditional 27:28-27:40 'The value depends on a condition']",
        "F12: unknown [Conditional 28:34-28:46 'The value depends on a condition']",
        "F13: unknown [Conditional 29:32-29:49 'The value depends on a condition']",
        "F14: unknown [Computed 30:28-30:37 'The value is computed by executable code']",
        "F15: known const(6:int)",
        "F16: known field(Values.Color.Red:Values.Color)",
        "F17: known field(Values.Color.Crimson:Values.Color)",
        "F18: unknown [Ambiguous 34:30-34:38 'The enum constant names several declared members']",
        "F19: unknown [Unsupported 35:30-35:38 'The enum constant names no declared member']",
        "F20: known field(Values.Color.Blue:Values.Color)",
        "F21: known const(:Values.Color?)",
        "F22: unknown [Unbound 38:28-38:35 'The value has no exact error-free binding']",
        "F23: unknown [Unbound 39:28-39:41 'The value has no exact error-free binding']",
        "F24: unknown [Dynamic 40:32-40:33 'The value is dynamically bound']",
        "F25: unknown [Unsupported 41:28-41:46 'The authored value conversion is not semantically valid']",
        "F26: unknown [Computed 42:31-42:42 'The value is computed by executable code']",
        "F27: known const(ab:string)",
        "F28: known field(Values.Plain.B:Values.Plain)",
        "F29: known const(c:char)",
        "F30: known const(1.5:decimal)",
        "F31: known payload(object){}",
        "F32: known const(0:int)",
        "F33: known const(2147483647:int)",
        "F34: known const(Known:string)",
        "F35: unknown [Unsupported 51:32-51:33 'The authored value conversion is not semantically valid']",
        "F36: unknown [Computed 52:28-52:35 'The value is computed by executable code']",
        "F37: unknown [Conditional 53:32-53:42 'The value depends on a condition']",
        "F38: unknown [Conditional 54:29-54:41 'The value depends on a condition']",
        "F39: known typeof(string)",
        "F40: unknown [Unsupported 56:36-56:63 'The authored value shape is outside bounded extraction']",
        "F41: unknown [Unbound 57:37-57:78 'The typeof operand has no exact error-free binding']",
        "F42: known const(1:int)",
        "F43: unknown [Conditional 59:28-59:65 'The value depends on a condition']",
        "F44: unknown [Computed 60:28-60:46 'The value is computed by executable code']",
        "F45: unknown [Unsupported 61:28-61:34 'The authored value conversion is not semantically valid']",
        "F46: unknown [Conditional 62:29-62:44 'The value depends on a condition']",
        "F47: unknown [Dynamic 63:36-63:53 'The typeof operand is dynamically bound']",
        "F48: known typeof(Values.Plain)",
        "F49: known const(5:int)",
        "F50: unknown [Unsupported 66:29-66:32 'The authored value conversion is not semantically valid']"
    ]);
}
