---
name: dotnet-orleans
version: "1.0.0"
category: "Data, Distributed, and AI"
description: "Build or review distributed .NET applications with Orleans grains, silos, streams, persistence, versioning, and cloud-native hosting patterns."
compatibility: "Requires Orleans projects or a serious distributed-state design discussion."
---

# Microsoft Orleans

## Trigger On

- building grain-based distributed systems in .NET
- reviewing silo configuration, storage, streams, reminders, or versioning
- adding Aspire orchestration or observability around Orleans services

## Workflow

1. Use Orleans when the grain model simplifies the domain; do not adopt it as a generic replacement for straightforward CRUD or queue workers.
2. Keep grain responsibilities narrow and state ownership explicit, including persistence, reminders, and stream consumers.
3. Review placement, serialization, versioning, and request context decisions because they affect cluster evolution and operability.
4. Use Aspire integration when it materially improves local orchestration, service discovery, and telemetry for the cluster.
5. Be deliberate about test strategy: unit tests for grain logic, plus realistic silo or in-process cluster tests for behavior under runtime rules.
6. Validate operational assumptions such as storage providers, clustering, and failure behavior, not only local happy-path execution.

## Deliver

- grain models and hosting setup that fit the domain
- clear provider and persistence choices
- operable Orleans services with realistic validation

## Validate

- Orleans is solving a distributed-state problem, not creating one
- grain boundaries and storage choices are explicit
- runtime behavior is tested beyond unit-only coverage
