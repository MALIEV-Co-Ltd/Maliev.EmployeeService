using Maliev.EmployeeService.Application.Sagas;
using Maliev.EmployeeService.Domain.Sagas;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Sagas;

public class EmployeeTerminationSagaTests
{
    [Fact]
    public void NewSaga_ShouldHaveCorrectStateMachineConfiguration()
    {
        var saga = new EmployeeTerminationSaga();

        Assert.NotNull(saga.EmployeeTerminated);
        Assert.NotNull(saga.LeaveBalanceClosed);
        Assert.NotNull(saga.CompensationArchived);
        Assert.NotNull(saga.AccessRevoked);
        Assert.NotNull(saga.LeaveClosureFaulted);
        Assert.NotNull(saga.CompensationArchivalFaulted);
        Assert.NotNull(saga.AccessRevocationFaulted);
        Assert.NotNull(saga.Processing);
        Assert.NotNull(saga.Completed);
        Assert.NotNull(saga.Faulted);
    }

    [Fact]
    public void Saga_ShouldHaveProperEventConfigurations()
    {
        var saga = new EmployeeTerminationSaga();

        Assert.NotNull(saga.Initial);
    }
}
