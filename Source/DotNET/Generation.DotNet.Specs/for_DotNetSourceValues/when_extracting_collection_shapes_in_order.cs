// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_extracting_collection_shapes_in_order : given.a_value_snapshot
{
    const string Source = """
        using System.Collections.Generic;
        using System.Collections.Immutable;

        namespace Values;

        public static class Usage
        {
            static bool Flag = true;
            static int Runtime() => 1;

            public static int[] C01 = new int[] { 1, 2 };
            public static int[] C02 = new[] { 1, 2 };
            public static int[] C03 = { 1, 2 };
            public static int[] C04 = [1, 2];
            public static List<int> C05 = [1, 2];
            public static List<int> C06 = new List<int> { 1, 2 };
            public static List<int> C07 = new() { 1, 2 };
            public static Dictionary<int, string> C08 = new() { { 1, "a" } };
            public static Dictionary<int, string> C09 = new() { [1] = "a" };
            public static int[] C10 = new int[2] { 1, 2 };
            public static int[] C11 = new int[2];
            public static int[] C12 = new int[Runtime()];
            public static int[] C13 = new int[Flag ? 1 : 2];
            public static int[,] C14 = new int[,] { { 1, 2 } };
            public static int[,] C15 = new[,] { { 1, 2 } };
            public static int[][] C16 = new int[][] { new[] { 1 }, new[] { 2 } };
            public static int[] C17 = new[] { 1, Runtime() };
            public static int[] C18 = [..C01];
            public static int[] C19 = [1, ..C01, ..C02];
            public static int[] C20 = new int[] { 1, Runtime(), Flag ? 1 : 2 };
            public static int[] C21 = new[] { Missing };
            public static object[] C22 = new object[] { 1, "a", typeof(string) };
            public static int[] C23 = new int[1L];
            public static int[] C24 = new int[-1];
            public static int[] C25 = new int[(uint)2];
            public static List<int> C26 = new List<int> { Runtime() };
            public static int[] C27 = new int[3] { 1, 2 };
            public static int[][] C28 = { new[] { 1 }, { 2 } };
            public static int[][] C29 = [[1], [2]];
            public static ImmutableArray<int> C30 = [1, 2];
            public static dynamic[] C31 = new dynamic[] { 1 };
            public static int[] C32 = new int[] { 1, 2, };
            public static Dictionary<int, string> C33 = new() { { 1, "a" }, { Runtime(), "b" } };
            public static Dictionary<int, string> C34 = new() { { 1, "a", "b" } };
            public static List<List<int>> C35 = new() { new() { 1 }, new() { 2 } };
            public static int[][] C36 = new int[][] { new[] { Runtime() }, { 2, Runtime() } };
            public static int[] C37 = new int[] { };
            public static int[] C38 = [];
            public static List<int> C39 = new List<int>();
            public static List<int> C40 = new List<int>(5) { 1 };
            public static List<int> C41 = new List<int> { Missing, Runtime() };
            public static int[,,] C42 = new int[1, 2, 3];
            public static int[,] C43 = new[,] { { 1, Runtime() }, { 2, 3 } };
            public static int[][] C44 = new int[2][];
            public static int[] C45 = new int[] { 1, "a" };
            public static IEnumerable<int> C46 = [1, 2];
            public static IEnumerable<int> C47 = new[] { 1, 2 };
            public static int[] C48 = new int[(Flag ? 1 : 2)];
            public static int[] C49 = new int[] { (Flag ? 1 : 2), 3 };
            public static List<int> C50 = new List<int> { 1, 2, Runtime() };
            public static HashSet<string> C51 = new() { "a", "a" };
        }
        """;

    string[] _snapshot = null!;

    void Because() => _snapshot = SnapshotOf("/workspace/Collections.cs", Source);

    [Fact]
    void should_snapshot_every_shape_in_declaration_order() => _snapshot.ShouldEqual(
    [
        "Flag: known const(True:bool)",
        "C01: known collection(int[])[10:42-10:43=const(1:int), 10:45-10:46=const(2:int)]",
        "C02: known collection(int[])[11:38-11:39=const(1:int), 11:41-11:42=const(2:int)]",
        "C03: known collection(int[])[12:32-12:33=const(1:int), 12:35-12:36=const(2:int)]",
        "C04: known collection(int[])[13:31-13:32=const(1:int), 13:34-13:35=const(2:int)]",
        "C05: known collection(System.Collections.Generic.List<int>)[14:35-14:36=const(1:int), 14:38-14:39=const(2:int)]",
        "C06: known collection(System.Collections.Generic.List<int>)[15:50-15:51=const(1:int), 15:53-15:54=const(2:int)]",
        "C07: known collection(System.Collections.Generic.List<int>)[16:42-16:43=const(1:int), 16:45-16:46=const(2:int)]",
        "C08: unknown [Unsupported 17:56-17:66 'A direct collection initializer entry must contain one authored value']",
        "C09: unknown [Unsupported 18:56-18:59 'The payload initializer is not a direct member assignment']",
        "C10: known collection(int[])[19:43-19:44=const(1:int), 19:46-19:47=const(2:int)]",
        "C11: unknown [Unsupported 20:30-20:40 'An array without an initializer has no individually authored elements'; Unsupported 20:38-20:39 'The explicit array dimension does not match its authored initializer elements']",
        "C12: unknown [Unsupported 21:30-21:48 'An array without an initializer has no individually authored elements'; Computed 21:38-21:47 'The array dimension is not one exact non-negative constant']",
        "C13: unknown [Unsupported 22:30-22:51 'An array without an initializer has no individually authored elements'; Conditional 22:38-22:50 'The array dimension depends on a condition']",
        "C14: unknown [Unsupported 23:35-23:41 'Only one-dimensional outer arrays are supported by bounded collection extraction']",
        "C15: unknown [Unsupported 24:31-24:50 'Only one-dimensional arrays are supported by bounded collection extraction']",
        "C16: known collection(int[][])[25:46-25:57=collection(int[])[25:54-25:55=const(1:int)], 25:59-25:70=collection(int[])[25:67-25:68=const(2:int)]]",
        "C17: unknown [Computed 26:41-26:50 'The value is computed by executable code']",
        "C18: unknown [OpaqueSpread 27:31-27:36 'A collection spread hides elements that are not authored individually'; Unsupported 27:33-27:36 'The authored value shape is outside bounded extraction']",
        "C19: unknown [OpaqueSpread 28:34-28:39 'A collection spread hides elements that are not authored individually'; Unsupported 28:36-28:39 'The authored value shape is outside bounded extraction'; OpaqueSpread 28:41-28:46 'A collection spread hides elements that are not authored individually'; Unsupported 28:43-28:46 'The authored value shape is outside bounded extraction']",
        "C20: unknown [Computed 29:45-29:54 'The value is computed by executable code'; Conditional 29:56-29:68 'The value depends on a condition']",
        "C21: unknown [Unbound 30:30-30:47 'The collection type has no exact error-free binding'; Unsupported 30:30-30:47 'The authored collection value conversion is not semantically valid'; Unbound 30:38-30:45 'The value has no exact error-free binding']",
        "C22: known collection(object[])[31:48-31:49=const(1:object), 31:51-31:54=const(a:object), 31:56-31:70=typeof(string)]",
        "C23: unknown [Unsupported 32:30-32:41 'An array without an initializer has no individually authored elements'; Unsupported 32:38-32:40 'The explicit array dimension does not match its authored initializer elements']",
        "C24: unknown [Unsupported 33:30-33:41 'An array without an initializer has no individually authored elements'; Computed 33:38-33:40 'The array dimension is not one exact non-negative constant']",
        "C25: unknown [Unsupported 34:30-34:46 'An array without an initializer has no individually authored elements'; Unsupported 34:38-34:45 'The explicit array dimension does not match its authored initializer elements']",
        "C26: unknown [Computed 35:50-35:59 'The value is computed by executable code']",
        "C27: unknown [Unsupported 36:38-36:39 'The explicit array dimension does not match its authored initializer elements']",
        "C28: unknown [Unsupported 37:47-37:52 'A nested collection initializer is outside bounded extraction']",
        "C29: known collection(int[][])[38:33-38:36=collection(int[])[38:34-38:35=const(1:int)], 38:38-38:41=collection(int[])[38:39-38:40=const(2:int)]]",
        "C30: known collection(System.Collections.Immutable.ImmutableArray<int>)[39:45-39:46=const(1:int), 39:48-39:49=const(2:int)]",
        "C31: unknown [Dynamic 40:34-40:53 'The collection type is dynamically bound'; Dynamic 40:50-40:51 'The value is dynamically bound']",
        "C32: known collection(int[])[41:42-41:43=const(1:int), 41:45-41:46=const(2:int)]",
        "C33: unknown [Unsupported 42:56-42:66 'A direct collection initializer entry must contain one authored value'; Unsupported 42:68-42:86 'A direct collection initializer entry must contain one authored value'; Computed 42:70-42:79 'The value is computed by executable code']",
        "C34: unknown [Unsupported 43:56-43:71 'A payload initializer contains a non-assignment element'; Unsupported 43:58-43:59 'The authored value conversion is not semantically valid'; Unsupported 43:61-43:64 'The authored value conversion is not semantically valid'; Unsupported 43:66-43:69 'The authored value conversion is not semantically valid']",
        "C35: known collection(System.Collections.Generic.List<System.Collections.Generic.List<int>>)[44:48-44:59=collection(System.Collections.Generic.List<int>)[44:56-44:57=const(1:int)], 44:61-44:72=collection(System.Collections.Generic.List<int>)[44:69-44:70=const(2:int)]]",
        "C36: unknown [Computed 45:54-45:63 'The value is computed by executable code'; Unsupported 45:67-45:83 'A nested collection initializer is outside bounded extraction'; Computed 45:72-45:81 'The value is computed by executable code']",
        "C37: known collection(int[])[]",
        "C38: known collection(int[])[]",
        "C39: known payload(System.Collections.Generic.List<int>){}",
        "C40: unknown [Unsupported 49:53-49:54 'A payload initializer contains a non-assignment element']",
        "C41: unknown [Unsupported 50:50-50:57 'A payload initializer contains a non-assignment element'; Unbound 50:50-50:57 'The value has no exact error-free binding'; Unsupported 50:59-50:68 'A payload initializer contains a non-assignment element'; Computed 50:59-50:68 'The value is computed by executable code']",
        "C42: unknown [Unsupported 51:32-51:48 'An array without an initializer has no individually authored elements'; Unsupported 51:36-51:48 'Only one-dimensional outer arrays are supported by bounded collection extraction']",
        "C43: unknown [Unsupported 52:31-52:68 'Only one-dimensional arrays are supported by bounded collection extraction'; Computed 52:45-52:54 'The value is computed by executable code']",
        "C44: unknown [Unsupported 53:32-53:44 'An array without an initializer has no individually authored elements'; Unsupported 53:40-53:41 'The explicit array dimension does not match its authored initializer elements']",
        "C45: unknown [Unsupported 54:45-54:48 'The authored value conversion is not semantically valid']",
        "C46: known collection(System.Collections.Generic.IEnumerable<int>)[55:42-55:43=const(1:int), 55:45-55:46=const(2:int)]",
        "C47: known collection(int[])[56:49-56:50=const(1:int), 56:52-56:53=const(2:int)]",
        "C48: unknown [Unsupported 57:30-57:53 'An array without an initializer has no individually authored elements'; Conditional 57:39-57:51 'The array dimension depends on a condition']",
        "C49: unknown [Conditional 58:43-58:55 'The value depends on a condition']",
        "C50: unknown [Computed 59:56-59:65 'The value is computed by executable code']",
        "C51: known collection(System.Collections.Generic.HashSet<string>)[60:48-60:51=const(a:string), 60:53-60:56=const(a:string)]"
    ]);
}
