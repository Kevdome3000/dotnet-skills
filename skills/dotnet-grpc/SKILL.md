---
name: dotnet-grpc
version: "1.0.0"
category: "Web and Cloud"
description: "Build or review gRPC services and clients in .NET with correct contract-first design, streaming behavior, transport assumptions, and backend service integration."
compatibility: "Requires ASP.NET Core gRPC or gRPC client projects."
---

# gRPC for .NET

## Trigger On

- building backend-to-backend RPC services or clients
- adding protobuf contracts, streaming calls, or interceptors
- deciding between gRPC, HTTP APIs, and SignalR

## Workflow

1. Use gRPC where low-latency backend communication, strong contracts, or streaming are the real drivers.
2. Treat `.proto` files as source of truth and keep generated code ownership clear.
3. Choose unary, server streaming, client streaming, or bidirectional streaming based on the interaction model, not by default.
4. Do not use gRPC for broad browser-facing APIs unless the limitations and gRPC-Web tradeoffs are explicitly acceptable.
5. Handle deadlines, cancellation, auth, and retry behavior explicitly on both server and client paths.
6. Validate contract changes carefully because gRPC drift breaks callers fast.

## Deliver

- stable protobuf contracts and generated code flow
- service and client code that match the RPC shape
- tests or smoke checks for serialization and call behavior

## Validate

- gRPC is chosen for the right problem
- streaming semantics and deadlines are explicit
- browser constraints are acknowledged when relevant
