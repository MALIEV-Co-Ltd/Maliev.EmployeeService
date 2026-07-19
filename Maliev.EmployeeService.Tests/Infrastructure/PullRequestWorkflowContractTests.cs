namespace Maliev.EmployeeService.Tests.WorkflowContracts;

public class PullRequestWorkflowContractTests
{
    [Fact]
    public void PullRequestWorkflow_ExposesProtectedMainAggregateGate()
    {
        var repositoryRoot = FindRepositoryRoot();
        var pullRequestWorkflow = File.ReadAllText(
            Path.Combine(repositoryRoot, ".github", "workflows", "pr-validation.yml"));
        var aggregateGatePath = Path.Combine(
            repositoryRoot,
            ".github",
            "workflows",
            "_protected-branch-gate.yml");

        Assert.True(File.Exists(aggregateGatePath), $"Missing aggregate gate workflow: {aggregateGatePath}");
        Assert.Contains("name: validate", pullRequestWorkflow, StringComparison.Ordinal);
        Assert.Contains("needs: [prep, build-and-test]", pullRequestWorkflow, StringComparison.Ordinal);
        Assert.Contains(
            "uses: ./.github/workflows/_protected-branch-gate.yml",
            pullRequestWorkflow,
            StringComparison.Ordinal);

        var aggregateGate = File.ReadAllText(aggregateGatePath);
        Assert.Contains("workflow_call:", aggregateGate, StringComparison.Ordinal);
        Assert.Contains("validate:", aggregateGate, StringComparison.Ordinal);
        Assert.Contains("name: validate", aggregateGate, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".github", "workflows")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the EmployeeService repository root.");
    }
}
