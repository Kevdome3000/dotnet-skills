---
name: dotnet-signalr
version: "1.0.0"
category: "Web and Cloud"
description: "Implement or review SignalR hubs, streaming, reconnection, transport, and real-time delivery patterns in ASP.NET Core applications."
compatibility: "Requires ASP.NET Core SignalR server or client code."
---

# SignalR

## Trigger On

- building chat, notification, collaboration, or live-update features
- debugging hub lifetime, connection state, or transport issues
- deciding whether SignalR or another transport better fits the scenario

## Workflow

1. Use SignalR for broadcast-style or connection-oriented real-time features; do not force gRPC into hub-style fan-out scenarios.
2. Model hub contracts intentionally and keep hub methods thin, delegating durable work elsewhere.
3. Plan for reconnection, backpressure, auth, and fan-out costs instead of treating real-time messaging as stateless request/response.
4. Use groups, presence, and connection metadata deliberately so scale-out behavior is understandable.
5. If Native AOT or trimming is in play, validate supported protocols and serialization choices explicitly.
6. Test connection behavior and failure modes, not just happy-path message delivery.

## Deliver

- clear hub contracts and connection behavior
- real-time delivery that matches the product scenario
- validation for reconnection and authorization flows

## Validate

- SignalR is the correct transport for the use case
- hub methods remain orchestration-oriented
- group and auth behavior are explicit and tested
