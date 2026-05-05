using Maliev.EmployeeService.Domain.Sagas;
using Maliev.MessagingContracts.Contracts.Compensation;
using Maliev.MessagingContracts.Contracts.Employee;
using Maliev.MessagingContracts.Contracts.Leave;
using Maliev.MessagingContracts.Contracts.Lifecycle;
using Maliev.MessagingContracts.Contracts.Shared;
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

        Event(() => EmployeeTerminated, x => x.CorrelateById(context => context.Message.Payload.EmployeeId));
        Event(() => LeaveBalanceClosed, x => x.CorrelateById(context => context.Message.Payload.EmployeeId));
        Event(() => CompensationArchived, x => x.CorrelateById(context => context.Message.Payload.EmployeeId));
        Event(() => AccessRevoked, x => x.CorrelateById(context => context.Message.Payload.EmployeeId));

        // Handle faults for compensating transactions
        Event(() => LeaveClosureFaulted, x => x.CorrelateById(context => context.Message.Message.EmployeeId));
        Event(() => CompensationArchivalFaulted, x => x.CorrelateById(context => context.Message.Message.EmployeeId));
        Event(() => AccessRevocationFaulted, x => x.CorrelateById(context => context.Message.Message.EmployeeId));

        Initially(
            When(EmployeeTerminated)
                .Then(context =>
                {
                    context.Saga.EmployeeId = context.Message.Payload.EmployeeId;
                    context.Saga.TerminationDate = context.Message.Payload.TerminationDate.DateTime;
                    context.Saga.CreatedDate = DateTime.UtcNow;
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
                    context.Saga.ModifiedDate = DateTime.UtcNow;
                })
                .Publish(context => new ArchiveCompensationCommand
                {
                    EmployeeId = context.Saga.EmployeeId
                }),

            When(CompensationArchived)
                .Then(context =>
                {
                    context.Saga.CompensationArchived = true;
                    context.Saga.ModifiedDate = DateTime.UtcNow;
                })
                .Publish(context => new RevokeAccessCommand
                {
                    EmployeeId = context.Saga.EmployeeId
                }),

            When(AccessRevoked)
                .Then(context =>
                {
                    context.Saga.AccessRevoked = true;
                    context.Saga.ModifiedDate = DateTime.UtcNow;
                })
                .TransitionTo(Completed)
        );

        // Compensating transactions for failures
        DuringAny(
            When(LeaveClosureFaulted)
                .Then(context =>
                {
                    // No previous steps to undo if the FIRST step fails
                })
                .TransitionTo(Faulted),

            When(CompensationArchivalFaulted)
                .Then(context =>
                {
                    // Undo Step 1 (Leave Balance Closure)
                })
                .Publish(context => CreateUndoCloseLeaveBalanceCommand(context.Saga.EmployeeId))
                .TransitionTo(Faulted),

            When(AccessRevocationFaulted)
                .Then(context =>
                {
                    // Undo Step 2 (Compensation Archival) and Step 1
                })
                .Publish(context => CreateUndoArchiveCompensationCommand(context.Saga.EmployeeId))
                .Publish(context => CreateUndoCloseLeaveBalanceCommand(context.Saga.EmployeeId))
                .TransitionTo(Faulted)
        );
    }

    public Event<EmployeeTerminatedEvent> EmployeeTerminated { get; private set; } = null!;
    public Event<LeaveBalanceClosedEvent> LeaveBalanceClosed { get; private set; } = null!;
    public Event<CompensationArchivedEvent> CompensationArchived { get; private set; } = null!;
    public Event<AccessRevokedEvent> AccessRevoked { get; private set; } = null!;

    // Fault events
    public Event<Fault<CloseLeaveBalanceCommand>> LeaveClosureFaulted { get; private set; } = null!;
    public Event<Fault<ArchiveCompensationCommand>> CompensationArchivalFaulted { get; private set; } = null!;
    public Event<Fault<RevokeAccessCommand>> AccessRevocationFaulted { get; private set; } = null!;

    public State Processing { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Faulted { get; private set; } = null!;

    private static UndoArchiveCompensationCommand CreateUndoArchiveCompensationCommand(Guid employeeId)
    {
        return new UndoArchiveCompensationCommand(
            MessageId: Guid.NewGuid(),
            MessageName: nameof(UndoArchiveCompensationCommand),
            MessageType: MessageType.Command,
            MessageVersion: "1.0",
            PublishedBy: "EmployeeService",
            ConsumedBy: ["CompensationService"],
            CorrelationId: employeeId,
            CausationId: null,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: new UndoArchiveCompensationCommandPayload(employeeId));
    }

    private static UndoCloseLeaveBalanceCommand CreateUndoCloseLeaveBalanceCommand(Guid employeeId)
    {
        return new UndoCloseLeaveBalanceCommand(
            MessageId: Guid.NewGuid(),
            MessageName: nameof(UndoCloseLeaveBalanceCommand),
            MessageType: MessageType.Command,
            MessageVersion: "1.0",
            PublishedBy: "EmployeeService",
            ConsumedBy: ["LeaveService"],
            CorrelationId: employeeId,
            CausationId: null,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: new UndoCloseLeaveBalanceCommandPayload(employeeId));
    }
}

// Internal command definitions for the Saga orchestration (compensation commands)
public class CloseLeaveBalanceCommand { public Guid EmployeeId { get; set; } public DateTime TerminationDate { get; set; } }
public class ArchiveCompensationCommand { public Guid EmployeeId { get; set; } }
public class RevokeAccessCommand { public Guid EmployeeId { get; set; } }
