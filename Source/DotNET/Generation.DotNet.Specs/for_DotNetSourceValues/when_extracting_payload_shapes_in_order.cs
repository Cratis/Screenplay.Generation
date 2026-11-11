// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_extracting_payload_shapes_in_order : given.a_value_snapshot
{
    const string Source = """
        namespace Values;

        public class Point
        {
            public Point()
            {
            }

            public Point(int x, int y)
            {
                X = x;
                Y = y;
            }

            public int X { get; set; }
            public int Y { get; set; }
            public int Ro { get; }
            public int Fld;
            public static int S { get; set; }
            public string Name { get; set; } = string.Empty;
            private int Hidden { get; set; }
            public int this[int index] { get => 0; set { } }
        }

        public class Base { public int B { get; set; } }

        public class Derived : Base { public int D { get; set; } }

        public abstract class Abs { }

        public record Pair(int First, string Second);

        public class Opt
        {
            public Opt(int a, int b = 2) { }
            public Opt(params long[] many) { }
        }

        public static class Usage
        {
            static bool Flag = true;
            static int Runtime() => 1;
            static string Name() => "a";

            public static Point P01 = new Point(1, 2);
            public static Point P02 = new Point(y: 2, x: 1);
            public static Point P03 = new Point { X = 1, Y = 2 };
            public static Point P04 = new Point(1, 2) { Name = "n" };
            public static Point P05 = new Point { X = 1, X = 2 };
            public static Point P06 = new Point { [0] = 1 };
            public static Point P07 = new Point { Ro = 1 };
            public static Point P08 = new Point { Fld = 1, X = Runtime() };
            public static Derived P09 = new Derived { B = 1, D = 2 };
            public static Pair P10 = new Pair(1, "a");
            public static Pair P11 = new Pair(Runtime(), "a");
            public static Pair P12 = new Pair(1);
            public static Opt P13 = new Opt(1);
            public static Opt P14 = new Opt(1, 2, 3);
            public static Abs P15 = new Abs();
            public static object P16 = new Missing(1);
            public static Point P17 = new Point { 1 };
            public static Point P18 = new Point { Hidden = 1 };
            public static Point P19 = new Point { S = 1 };
            public static Point P20 = new(1, 2);
            public static Pair P21 = new Pair(Second: "a", First: 1);
            public static Point P22 = new Point { X = Flag ? 1 : 2 };
            public static Point P23 = new Point { X = "a" };
            public static Pair P24 = new Pair(Runtime(), Name());
            public static Point P25 = new Point { [Runtime()] = 1 };
            public static Point P26 = new Point { X = 1, Y = Missing };
            public static Point P27 = new Point(1, 2) { X = 3, Y = Runtime(), Name = "n" };
            public static Point P28 = new Point { Fld = 1, Fld = 2, X = 1, X = Runtime() };
            public static object P29 = new Point { X = 1 };
            public static Pair P30 = new Pair(First: 1, Second: "a", Third: 2);
            public static Point P31 = new Point { X = 1, Y = 2, Missing = 3 };
            public static Point P32 = new Point(1, Runtime()) { Name = Name() };
            public static Point P33 = new Point { Name = $"{Runtime()}" };
            public static Point P34 = new Point { Y = 1L };
            public static Point P35 = new Point { X = (Flag ? 1 : 2) };
            public static Pair P36 = new Pair(1, "a") { };
            public static Derived P37 = new Derived { D = 2, B = 1 };
            public static Point P38 = new Point { X = 1, [0] = Runtime(), Y = 2 };
            public static Point P39 = new Point { 1, X = 2 };
            public static Holder P40 = new Holder { W = 5 };
            public static Holder P41 = new Holder { W = 5, W = 6 };
        }

        public readonly struct Wrapped
        {
            public static implicit operator Wrapped(int value) => default;
        }

        public class Holder
        {
            public Wrapped W { get; set; }
        }
        """;

    string[] _snapshot = null!;

    void Because() => _snapshot = SnapshotOf("/workspace/Payloads.cs", Source);

    [Fact] void should_snapshot_every_shape_in_declaration_order() => _snapshot.ShouldEqual(
    [
        "Flag: known const(True:bool)",
        "P01: known payload(Values.Point){x@44:40-44:41=const(1:int), y@44:43-44:44=const(2:int)}",
        "P02: known payload(Values.Point){x@45:49-45:50=const(1:int), y@45:43-45:44=const(2:int)}",
        "P03: known payload(Values.Point){X@46:46-46:47=const(1:int), Y@46:53-46:54=const(2:int)}",
        "P04: known payload(Values.Point){x@47:40-47:41=const(1:int), y@47:43-47:44=const(2:int), Name@47:55-47:58=const(n:string)}",
        "P05: unknown [DuplicateMember 48:53-48:54 'Payload member 'X' is assigned more than once']",
        "P06: unknown [Unsupported 49:42-49:45 'The payload initializer is not a direct member assignment']",
        "P07: unknown [Unsupported 50:42-50:44 'The payload initializer is not a direct member assignment']",
        "P08: unknown [Computed 51:55-51:64 'The value is computed by executable code']",
        "P09: known payload(Values.Derived){B@52:50-52:51=const(1:int), D@52:57-52:58=const(2:int)}",
        "P10: known payload(Values.Pair){First@53:38-53:39=const(1:int), Second@53:41-53:44=const(a:string)}",
        "P11: unknown [Computed 54:38-54:47 'The value is computed by executable code']",
        "P12: unknown [Unbound 55:29-55:40 'The payload construction has no exact bound constructor and type'; Unsupported 55:38-55:39 'The authored value conversion is not semantically valid']",
        "P13: unknown [Unsupported 56:35-56:38 'Every payload constructor parameter must be stated by one exact authored argument']",
        "P14: unknown [Unsupported 57:35-57:44 'Every payload constructor parameter must be stated by one exact authored argument']",
        "P15: unknown [Unsupported 58:28-58:37 'The construction root is not semantically valid']",
        "P16: unknown [Unbound 59:31-59:45 'The payload type has no exact error-free binding'; Unsupported 59:43-59:44 'The authored value conversion is not semantically valid']",
        "P17: unknown [Unsupported 60:30-60:45 'The construction root is not semantically valid'; Unsupported 60:42-60:43 'A payload initializer contains a non-assignment element']",
        "P18: unknown [Unsupported 61:42-61:48 'The payload initializer is not a direct member assignment']",
        "P19: unknown [Unsupported 62:42-62:43 'The payload initializer is not a direct member assignment']",
        "P20: known payload(Values.Point){x@63:34-63:35=const(1:int), y@63:37-63:38=const(2:int)}",
        "P21: known payload(Values.Pair){First@64:58-64:59=const(1:int), Second@64:46-64:49=const(a:string)}",
        "P22: unknown [Conditional 65:46-65:58 'The value depends on a condition']",
        "P23: unknown [Unsupported 66:46-66:49 'The payload value conversion is not semantically valid']",
        "P24: unknown [Computed 67:38-67:47 'The value is computed by executable code'; Computed 67:49-67:55 'The value is computed by executable code']",
        "P25: unknown [Unsupported 68:42-68:53 'The payload initializer is not a direct member assignment'; Computed 68:43-68:52 'The value is computed by executable code']",
        "P26: unknown [Unsupported 69:53-69:60 'The payload value conversion is not semantically valid'; Unbound 69:53-69:60 'The value has no exact error-free binding']",
        "P27: unknown [Computed 70:59-70:68 'The value is computed by executable code']",
        "P28: unknown [DuplicateMember 71:57-71:58 'Payload member 'Fld' is assigned more than once'; DuplicateMember 71:71-71:80 'Payload member 'X' is assigned more than once'; Computed 71:71-71:80 'The value is computed by executable code']",
        "P29: known payload(Values.Point){X@72:47-72:48=const(1:int)}",
        "P30: unknown [Unbound 73:29-73:70 'The payload construction has no exact bound constructor and type'; Unsupported 73:45-73:46 'The authored value conversion is not semantically valid'; Unsupported 73:56-73:59 'The authored value conversion is not semantically valid'; Unsupported 73:68-73:69 'The authored value conversion is not semantically valid']",
        "P31: unknown [Unsupported 74:56-74:63 'The payload initializer is not a direct member assignment']",
        "P32: unknown [Computed 75:43-75:52 'The value is computed by executable code'; Computed 75:63-75:69 'The value is computed by executable code']",
        "P33: unknown [Computed 76:49-76:63 'The value is computed by executable code']",
        "P34: unknown [Unsupported 77:46-77:48 'The payload value conversion is not semantically valid']",
        "P35: unknown [Conditional 78:47-78:59 'The value depends on a condition']",
        "P36: known payload(Values.Pair){First@79:38-79:39=const(1:int), Second@79:41-79:44=const(a:string)}",
        "P37: known payload(Values.Derived){D@80:50-80:51=const(2:int), B@80:57-80:58=const(1:int)}",
        "P38: unknown [Unsupported 81:49-81:52 'The payload initializer is not a direct member assignment'; Computed 81:55-81:64 'The value is computed by executable code']",
        "P39: unknown [Unsupported 82:42-82:43 'A payload initializer contains a non-assignment element']",
        "P40: unknown [Unsupported 83:48-83:49 'The payload value conversion is not semantically valid']",
        "P41: unknown [Unsupported 84:48-84:49 'The payload value conversion is not semantically valid'; DuplicateMember 84:55-84:56 'Payload member 'W' is assigned more than once'; Unsupported 84:55-84:56 'The payload value conversion is not semantically valid']"
    ]);
}
