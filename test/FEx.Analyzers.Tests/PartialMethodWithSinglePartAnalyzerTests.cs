using Microsoft.CodeAnalysis.CSharp;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class PartialMethodWithSinglePartAnalyzerTests
{
    private static Task VerifyAsync(string source, LanguageVersion? languageVersion = null, string[]? otherFiles = null) =>
        AnalyzerTestHelper.VerifyAsync<PartialMethodWithSinglePartAnalyzer>(source, languageVersion: languageVersion, otherFiles: otherFiles);

    [Fact]
    public Task Ignores_E07_partial_class_with_partial_method_that() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class E07 { public partial void M(); }
            public partial class E07 { public partial void M() { } }
            """);

    [Fact]
    public Task Reports_S01_oracle_partial_method_single_part_used() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class S01 { {|FEX0044:partial|} void Only(); public void G() => Only(); }
            """);

    [Fact]
    public Task Reports_S02_partial_method_single_part_never_called() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class S02 { {|FEX0044:partial|} void Only(); }
            """);

    [Fact]
    public Task Ignores_S03_two_parts() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class S03 { partial void M(); public void G() => M(); }
            public partial class S03 { partial void M() { Console.Write(1); } }
            """);

    [Fact]
    public Task Ignores_S05_extended_partial_with_implementation() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class S05 { public partial int M(); }
            public partial class S05 { public partial int M() => 1; }
            """);

    [Fact]
    public Task Ignores_S07_oracle_partial_type_single() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class S07 { }
            """);

    [Fact]
    public Task Ignores_S08_partial_struct_single() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public partial struct S08 { }
            """);

    [Fact]
    public Task Ignores_S09_partial_interface_single() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public partial interface S09 { }
            """);

    [Fact]
    public Task Ignores_S10_partial_record_single() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public partial record S10;
            """);

    [Fact]
    public Task Ignores_S11_partial_nested_single() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class S11 { public partial class In { } }
            """);

    [Fact]
    public Task Ignores_S12_partial_static_class_single() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public static partial class S12 { }
            """);

    [Fact]
    public Task Ignores_S13_partial_generic_single() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class S13<T> { }
            """);

    [Fact]
    public Task Ignores_S14_two_parts_class() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class S14 { }
            public partial class S14 { }
            """);

    [Fact]
    public Task Ignores_S15_partial_class_second_part_in_another() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class S15 { }
            """, otherFiles: ["public partial class S15 { }"]);

    [Fact]
    public Task Ignores_S16_single_part_with_partial_method_impl() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class S16 { partial void M(); partial void M() { } }
            """);

    [Fact]
    public Task Ignores_S17_partial_property() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class S17 { public partial int P { get; set; } }
            public partial class S17 { public partial int P { get => 1; set { } } }
            """);

    [Fact]
    public Task Ignores_S18_partial_record_struct_single() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public partial record struct S18;
            """);

    [Fact]
    public Task Ignores_S19_partial_class_with_members_single() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class S19 { public int F; }
            """);

    [Fact]
    public Task Ignores_S20_partial_type_parts_in_nested() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class S20 { public partial class In { } }
            public partial class S20 { }
            """);

    [Fact]
    public Task Ignores_S22_partial_method_with_parameters_declared_with() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class S22 { partial void M(int a); public void G() => M(1); }
            public partial class S22 { partial void M(int a) { Console.Write(a); } }
            """);

    [Fact]
    public Task Ignores_S23_partial_class_implementing_interface_single() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class S23 : IDisposable { public void Dispose() { } }
            """);

    [Fact]
    public Task Ignores_S25_partial_class_with_attributes() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            [Obsolete]
            public partial class S25 { }
            """);

    [Fact]
    public Task Reports_SA1_partial_method_decl_with_attributes_only() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class SA1 { [Obsolete] {|FEX0044:partial|} void M(); }
            """);

    [Fact]
    public Task Reports_SA2_partial_method_in_partial_struct() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public partial struct SA2 { {|FEX0044:partial|} void M(); }
            """);

    [Fact]
    public Task Reports_SA3_partial_method_in_generic_class() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class SA3<T> { {|FEX0044:partial|} void M(T t); public T? F; }
            """);

    [Fact]
    public Task Ignores_SA4_partial_private_with_impl_in_same() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class SA4 { partial void M(); partial void M() { } }
            """);

    [Fact]
    public Task Reports_SA5_partial_method_in_nested_partial_type() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public class SA5 { public partial class In { {|FEX0044:partial|} void M(); } }
            """);

    [Fact]
    public Task Reports_SA7_partial_class_a_source_file() =>
        VerifyAsync("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Runtime.InteropServices;
            using System.Threading;
            using System.Threading.Tasks;

            public partial class SA7 { {|FEX0044:partial|} void M(); }
            """);
}
