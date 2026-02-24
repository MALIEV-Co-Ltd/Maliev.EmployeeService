using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

/// <summary>
/// Defines a test collection for integration tests.
/// All tests in this collection will run sequentially and share the same test containers.
/// </summary>
[CollectionDefinition("IntegrationTests", DisableParallelization = true)]
public class IntegrationTestCollection
{
    // This class is never instantiated. It exists solely to define the collection.
}
