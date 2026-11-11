// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_function_pointer_types_are_not_exact : given.a_value_snapshot
{
    string[] _snapshot = null!;

    void Because() => _snapshot = SnapshotOf(
        "/workspace/FunctionPointers.cs",
        """
        namespace Values;

        public static unsafe class Usage
        {
            public static readonly System.Type Exact = typeof(delegate*<int, string>);
            public static readonly System.Type ReturnFailure = typeof(delegate*<int, Missing>);
            public static readonly System.Type ParameterFailure = typeof(delegate*<Missing, int>);
            public static readonly System.Type SecondParameterFailure = typeof(delegate*<int, Missing, int>);
            public static readonly System.Type BothFail = typeof(delegate*<Missing, Other>);
            public static readonly System.Type NestedReturnFailure = typeof(delegate*<delegate*<Missing>, int>);
            public static readonly System.Type NestedParameterFailure = typeof(delegate*<int, delegate*<Missing, int>>);
            public static readonly System.Type DynamicParameter = typeof(delegate*<dynamic, int>);
            public static readonly System.Type DynamicReturn = typeof(delegate*<int, dynamic>);
            public static readonly System.Type DynamicParameterBeatsReturnFailure = typeof(delegate*<dynamic, Missing>);
            public static readonly System.Type DynamicReturnBeatsParameterFailure = typeof(delegate*<Missing, dynamic>);
        }
        """,
        allowUnsafe: true);

    [Fact]
    void should_snapshot_each_function_pointer_signature_position() => string.Join('\n', _snapshot).ShouldEqual(
        "Exact: known typeof(delegate*<int, string>)\n" +
        "ReturnFailure: unknown [Unbound 5:55-5:86 'The typeof operand has no exact error-free binding']\n" +
        "ParameterFailure: unknown [Unbound 6:58-6:89 'The typeof operand has no exact error-free binding']\n" +
        "SecondParameterFailure: unknown [Unbound 7:64-7:100 'The typeof operand has no exact error-free binding']\n" +
        "BothFail: unknown [Unbound 8:50-8:83 'The typeof operand has no exact error-free binding']\n" +
        "NestedReturnFailure: unknown [Unbound 9:61-9:103 'The typeof operand has no exact error-free binding']\n" +
        "NestedParameterFailure: unknown [Unbound 10:64-10:111 'The typeof operand has no exact error-free binding']\n" +
        "DynamicParameter: unknown [Dynamic 11:58-11:89 'The typeof operand is dynamically bound']\n" +
        "DynamicReturn: unknown [Dynamic 12:55-12:86 'The typeof operand is dynamically bound']\n" +
        "DynamicParameterBeatsReturnFailure: unknown [Dynamic 13:76-13:111 'The typeof operand is dynamically bound']\n" +
        "DynamicReturnBeatsParameterFailure: unknown [Dynamic 14:76-14:111 'The typeof operand is dynamically bound']");
}
