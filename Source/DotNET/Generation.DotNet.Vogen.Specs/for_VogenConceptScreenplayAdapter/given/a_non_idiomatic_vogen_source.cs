// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.Vogen.given;

public class a_non_idiomatic_vogen_source : a_vogen_compilation
{
    protected DotNetProjectCompilation SourceProject = null!;

    void Establish()
    {
        var compilation = CompilationFrom(
            "Concepts",
            new SourceFile(
                "/workspace/Concepts/CustomerCode.cs",
                """
                namespace Concepts;
                [global::Vogen.ValueObjectAttribute<global::System.String>]
                public readonly partial record struct CustomerCode
                {
                    private static global::Vogen.Validation Validate(global::System.String value)
                    {
                        return ((global::System.String)value).Length > 0
                            ? global::Vogen.Validation.Ok
                            : global::Vogen.Validation.Invalid((global::System.String)"Customer code is required");
                    }
                }
                """));
        SourceProject = Project("Concepts.Project", compilation);
    }
}
