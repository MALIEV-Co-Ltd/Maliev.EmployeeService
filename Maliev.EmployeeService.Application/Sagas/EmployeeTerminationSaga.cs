using Maliev.EmployeeService.Domain.IntegrationEvents;
using Maliev.EmployeeService.Domain.Sagas;
using MassTransit;

namespace Maliev.EmployeeService.Application.Sagas;

/// <summary>
/// Orchestrates the distributed transaction for employee termination across multiple microservices.
/// Coordination logic for User Story 1 (Employee Service) integration with Leave, Compensation, and Lifecycle services.
/// </summary>
public class EmployeeTerminationSaga : MassTransitStateMachine<EmployeeTerminationSagaState>
{
    public EmployeeTerminationSaga()
    {
        InstanceState(x => x.CurrentState);

        Event(() => EmployeeTerminated, x => x.CorrelateById(context => context.Message.EmployeeId));
        Event(() => LeaveBalanceClosed, x => x.CorrelateById(context => context.Message.EmployeeId));
        Event(() => CompensationArchived, x => x.CorrelateById(context => context.Message.EmployeeId));
        Event(() => AccessRevoked, x => x.CorrelateById(context => context.Message.EmployeeId));

        // Handle faults for compensating transactions
        Event(() => LeaveClosureFaulted, x => x.CorrelateById(context => context.Message.Message.EmployeeId));

        Initially(
            When(EmployeeTerminated)
                .Then(context =>
                {
                    context.Saga.EmployeeId = context.Message.EmployeeId;
                    context.Saga.TerminationDate = context.Message.TerminationDate;
                    context.Saga.CreatedAt = DateTime.UtcNow;
                })
                .Publish(context => new CloseLeaveBalanceCommand
                {
                    EmployeeId = context.Saga.EmployeeId,
                    TerminationDate = context.Saga.TerminationDate
                })
                .TransitionTo(Processing)
        );

        During(Processing,
            When(LeaveBalanceClosed)
                .Then(context =>
                {
                    context.Saga.LeaveBalanceClosed = true;
                    context.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .Publish(context => new ArchiveCompensationCommand
                {
                    EmployeeId = context.Saga.EmployeeId
                }),

            When(CompensationArchived)
                .Then(context =>
                {
                    context.Saga.CompensationArchived = true;
                    context.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .Publish(context => new RevokeAccessCommand
                {
                    EmployeeId = context.Saga.EmployeeId
                }),

            When(AccessRevoked)
                .Then(context =>
                {
                    context.Saga.AccessRevoked = true;
                    context.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .TransitionTo(Completed)
        );

        // Compensating transactions for failures
        DuringAny(
            When(LeaveClosureFaulted)
                .Then(context =>
                {
                    // Log failure and initiate cleanup if needed
                })
                .TransitionTo(Faulted)
        );
    }

    public Event<EmployeeTerminatedIntegrationEvent> EmployeeTerminated { get; private set; } = null!;
    public Event<LeaveBalanceClosedEvent> LeaveBalanceClosed { get; private set; } = null!;
    public Event<CompensationArchivedEvent> CompensationArchived { get; private set; } = null!;
    public Event<AccessRevokedEvent> AccessRevoked { get; private set; } = null!;

    // Fault events
    public Event<Fault<CloseLeaveBalanceCommand>> LeaveClosureFaulted { get; private set; } = null!;

    public State Processing { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Faulted { get; private set; } = null!;
}

// Internal command/event definitions for the Saga orchestration
public class CloseLeaveBalanceCommand { public Guid EmployeeId { get; set; } public DateTime TerminationDate { get; set; } }
public class ArchiveCompensationCommand { public Guid EmployeeId { get; set; } }
public class RevokeAccessCommand { public Guid EmployeeId { get; set; } }

public class LeaveBalanceClosedEvent { public Guid EmployeeId { get; set; } }
public class CompensationArchivedEvent { public Guid EmployeeId { get; set; } }
public class AccessRevokedEvent { public Guid EmployeeId { get; set; } }