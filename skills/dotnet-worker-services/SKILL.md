---
name: dotnet-worker-services
version: "1.0.0"
category: "Web and Cloud"
description: "Build long-running .NET background services with `BackgroundService`, Generic Host, graceful shutdown, configuration, logging, and deployment patterns suited to workers and daemons."
compatibility: "Requires a worker, hosted service, or background-processing scenario."
---

# .NET Worker Services

## Trigger On

- building long-running background services or scheduled workers
- adding hosted services to an app or extracting them into a worker process
- reviewing graceful shutdown, cancellation, queue processing, or health behavior

## Workflow

1. Use the Worker SDK and Generic Host patterns instead of ad-hoc forever loops in console apps.
2. Propagate cancellation tokens and implement graceful shutdown deliberately so work is drained or stopped predictably.
3. Keep the execution loop thin and move business logic into testable services.
4. Treat idempotency, retry policy, and poison-message behavior as first-class concerns for queue or timer work.
5. Review server GC, hosting mode, and deployment environment when throughput or memory pressure matters.
6. Use logs, metrics, and health endpoints or heartbeat signals so background work is observable in production.

## Deliver

- well-behaved worker processes and hosted services
- predictable startup and shutdown behavior
- observability and retry patterns that match the workload

## Validate

- cancellation and shutdown are honored
- the worker loop is testable and not over-coupled
- runtime behavior is visible through logs or telemetry
