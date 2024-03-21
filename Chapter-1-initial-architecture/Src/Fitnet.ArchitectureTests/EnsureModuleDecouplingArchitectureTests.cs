namespace EvolutionaryArchitecture.Fitnet.ArchitectureTests;

using Common;

public class EnsureModuleDecouplingArchitectureTests
{
    [Theory]
    [MemberData(nameof(GetAllDistinctPairsOfModuleNamespaces))]
    internal void Modules_should_not_depend_on_each_other_except_for_events(string keyModule, string referencingModuleName)
    {
        // Arrange
        var offersModule = Solution.Types
            .That()
            .ResideInNamespace(keyModule);

        var forbiddenModule = Solution.Types
            .That()
            .ResideInNamespace(referencingModuleName)
            .And()
            .DoNotHaveNameEndingWith("Event");
        var forbiddenModuleTypes = forbiddenModule.GetModuleTypes();

        // Act
        var rules = offersModule
            .Should()
            .NotHaveDependencyOnAny(forbiddenModuleTypes);
        var validationResult = rules!.GetResult();

        // Assert
        validationResult.FailingTypes.Should().BeNull();
    }

    public static IEnumerable<object[]> GetAllDistinctPairsOfModuleNamespaces()
    {
        var baseNamespace = "EvolutionaryArchitecture.Fitnet";
        var baseNamespaceSegments = baseNamespace.Count(c => c == '.') + 1;

        var filteredModuleNamespaces = Solution.Types
            .That()
            .ResideInNamespaceStartingWith(baseNamespace)
            .GetTypes()
            .Select(type => type.Namespace)
            .Distinct()
            .Where(namespaceName =>
                namespaceName!.Count(c => c == '.') == baseNamespaceSegments &&
                !namespaceName!.EndsWith(".Common", StringComparison.InvariantCulture) &&
                !namespaceName!.EndsWith(".Migrations", StringComparison.InvariantCulture))
            //.Select(primary => primary!.Split(".")[^1])
            .ToList();

        return filteredModuleNamespaces.SelectMany(
            moduleA => filteredModuleNamespaces.Where(moduleB => moduleB != moduleA),
            (moduleA, moduleB) => new object[] { moduleA!, moduleB! });
    }
}
