using System.Threading.Tasks;
using Xunit;

namespace FEx.Analyzers.Tests;

public class RedundantCatchClauseAnalyzerTests
{
    private static Task VerifyAsync(string source) => AnalyzerTestHelper.VerifyAsync<RedundantCatchClauseAnalyzer>(source);

    [Fact]
    public Task Reports_a_sole_catch_that_only_rethrows() =>
        VerifyAsync("""
            using System;
            class C
            {
                void Typed() { try { Run(); } {|FEX0035:catch (Exception) { throw; }|} }
                void Variable() { try { Run(); } {|FEX0035:catch (Exception e) { throw; }|} }
                void Specific() { try { Run(); } {|FEX0035:catch (ArgumentException) { throw; }|} }
                void Bare() { try { Run(); } {|FEX0035:catch { throw; }|} }
                void Run() { }
            }
            """);

    [Fact]
    public Task Reports_a_rethrow_next_to_a_finally() =>
        VerifyAsync("""
            using System;
            class C
            {
                void M() { try { Run(); } {|FEX0035:catch { throw; }|} finally { Run(); } }
                void Run() { }
            }
            """);

    [Fact]
    public Task Reports_a_rethrow_of_one_type_followed_by_an_unrelated_catch() =>
        VerifyAsync("""
            using System;
            using System.IO;
            class C
            {
                void M() { try { Run(); } {|FEX0035:catch (IOException) { throw; }|} catch (ArgumentException) { Run(); } }
                void Run() { }
            }
            """);

    [Fact]
    public Task Reports_a_rethrow_after_a_catch_that_does_work() =>
        VerifyAsync("""
            using System;
            using System.IO;
            class C
            {
                void M() { try { Run(); } catch (IOException) { Run(); } {|FEX0035:catch (Exception) { throw; }|} }
                void Run() { }
            }
            """);

    [Fact]
    public Task Reports_every_rethrow_when_the_later_ones_only_rethrow_too() =>
        VerifyAsync("""
            using System;
            using System.IO;
            class C
            {
                void Narrow() { try { Run(); } {|FEX0035:catch (IOException) { throw; }|} {|FEX0035:catch (Exception) { throw; }|} }
                void Bare() { try { Run(); } {|FEX0035:catch (IOException) { throw; }|} {|FEX0035:catch { throw; }|} }
                void Finally() { try { Run(); } {|FEX0035:catch (FileNotFoundException) { throw; }|} {|FEX0035:catch (IOException) { throw; }|} finally { Run(); } }
                void Run() { }
            }
            """);

    [Fact]
    public Task Reports_a_rethrow_in_a_nested_try_and_in_a_lambda() =>
        VerifyAsync("""
            using System;
            class C
            {
                void Nested() { try { try { Run(); } {|FEX0035:catch (Exception) { throw; }|} } finally { Run(); } }
                Action Lambda() => () => { try { Run(); } {|FEX0035:catch (Exception) { throw; }|} };
                void Run() { }
            }
            """);

    [Fact]
    public Task Ignores_a_rethrow_that_keeps_a_later_catch_from_seeing_the_exception() =>
        VerifyAsync("""
            using System;
            using System.IO;
            class C
            {
                void Broad() { try { Run(); } catch (IOException) { throw; } catch (Exception) { Run(); } }
                void Bare() { try { Run(); } catch (IOException) { throw; } catch { Run(); } }
                void BareAfterBroad() { try { Run(); } catch (Exception) { throw; } catch { Run(); } }
                void Filtered(bool b) { try { Run(); } catch (IOException) { throw; } catch (Exception) when (b) { Run(); } }
                void FilteredRethrow(bool b) { try { Run(); } catch (IOException) { throw; } catch (Exception) when (b) { throw; } }
                void Run() { }
            }
            """);

    [Fact]
    public Task Ignores_a_catch_that_does_more_than_rethrow() =>
        VerifyAsync("""
            using System;
            class C
            {
                void Work() { try { Run(); } catch (Exception) { Run(); throw; } }
                void Wrapped() { try { Run(); } catch (Exception e) { throw new InvalidOperationException("x", e); } }
                void Named() { try { Run(); } catch (Exception e) { throw e; } }
                void Empty() { try { Run(); } catch (Exception) { } }
                void Nested() { try { Run(); } catch (Exception) { { throw; } } }
                void Twice() { try { Run(); } catch (Exception) { throw;; } }
                void Run() { }
            }
            """);

    [Fact]
    public Task Ignores_a_catch_with_a_filter() =>
        VerifyAsync("""
            using System;
            class C
            {
                void M(bool b) { try { Run(); } catch (Exception) when (b) { throw; } }
                void Run() { }
            }
            """);

    [Fact]
    public Task Ignores_a_rethrow_before_a_catch_of_a_type_it_cannot_resolve() =>
        VerifyAsync("""
            using System;
            class C
            {
                void M() { try { Run(); } catch (ArgumentException) { throw; } catch ({|CS0246:Missing|}) { Run(); } }
                void Run() { }
            }
            """);

    [Fact]
    public Task Ignores_a_rethrow_of_a_type_it_cannot_resolve() =>
        VerifyAsync("""
            using System;
            class C
            {
                void M() { try { Run(); } catch ({|CS0246:Missing|}) { throw; } catch (Exception) { Run(); } }
                void Run() { }
            }
            """);

    [Fact]
    public Task Ignores_a_try_without_a_catch() =>
        VerifyAsync("""
            class C
            {
                void M() { try { Run(); } finally { Run(); } }
                void Run() { }
            }
            """);
}
