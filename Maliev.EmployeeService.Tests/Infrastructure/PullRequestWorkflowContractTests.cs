namespace Maliev.EmployeeService.Tests.WorkflowContracts;

public class PullRequestWorkflowContractTests
{
    [Fact]
    public void BuildWorkflow_UsesCredentialFreePinnedSharedSources()
    {
        var repositoryRoot = FindRepositoryRoot();
        var workflow = File.ReadAllText(
            Path.Combine(repositoryRoot, ".github", "workflows", "_build-and-test.yml"));

        Assert.Contains("repository: MALIEV-Co-Ltd/Maliev.Aspire", workflow, StringComparison.Ordinal);
        Assert.Contains("repository: MALIEV-Co-Ltd/Maliev.MessagingContracts", workflow, StringComparison.Ordinal);
        Assert.Contains("-p:GITHUB_ACTIONS=false", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("NUGET_PASSWORD", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("secrets.gitops_pat", workflow, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ci-develop.yml")]
    [InlineData("ci-staging.yml")]
    [InlineData("ci-main.yml")]
    public void BranchPushWorkflow_IsValidationOnly(string workflowName)
    {
        var repositoryRoot = FindRepositoryRoot();
        var workflow = File.ReadAllText(
            Path.Combine(repositoryRoot, ".github", "workflows", workflowName));

        Assert.Contains("permissions:", workflow, StringComparison.Ordinal);
        Assert.Contains("contents: read", workflow, StringComparison.Ordinal);
        Assert.Contains("build-and-test:", workflow, StringComparison.Ordinal);
        Assert.Contains(
            "uses: ./.github/workflows/_build-and-test.yml",
            workflow,
            StringComparison.Ordinal);
        Assert.DoesNotContain("deploy:", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("google-github-actions/auth", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("gcloud", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("docker push", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("maliev-gitops", workflow, StringComparison.Ordinal);
    }

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
