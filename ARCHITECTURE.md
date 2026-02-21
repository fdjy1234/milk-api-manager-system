# System Architecture

## Overview
Milk API Manager System is an enterprise-grade API management solution built on .NET 8, acting as the control plane for Apache APISIX. It features strict security controls, audit logging, and deep integration with enterprise identity (LDAP).

## Architecture Diagram

```mermaid
graph TD
    User[User / Client] -->|HTTPS| APISIX[Apache APISIX Gateway]
    Admin[Admin User] -->|HTTPS| Blazor[Admin UI (Blazor Server)]
    
    subgraph "Control Plane"
        Blazor -->|Internal API| Backend[.NET 8 Backend API]
        Backend -->|Admin API| APISIX
        Backend -->|SQL| DB[(PostgreSQL 17)]
        Backend -->|LDAP| AD[Active Directory]
    end

    subgraph "Data Plane"
        APISIX -->|Proxy| Upstream[Upstream Services]
        APISIX -.->|Metrics| Prom[Prometheus]
    end
```

## Core Components

### 1. Backend API (.NET 8)
-   **Role**: Central control plane.
-   **Database**: PostgreSQL 17 (Entity Framework Core).
-   **Auth**: JWT-based auth with LDAP integration (Novell.Directory.Ldap).
-   **Features**:
    -   **Route Management**: Syncs routes to APISIX.
    -   **Security**: Manages Per-Route Whitelists and Global Blacklists.
    -   **Audit**: Logs all configuration changes (Action, User, Resource, Details).

### 2. Apache APISIX
-   **Role**: High-performance API Gateway.
-   **Plugins Used**:
    -   `ip-restriction`: Managed via Backend (Whitelist).
    -   `traffic-blocker`: Managed via Backend (Blacklist).
    -   `pii-masker`: Custom Lua plugin for masking sensitive data (e.g., Credit Cards, Phones).
    -   `prometheus`: Exposes metrics (including `pii_masked_total`).

### 3. Database Schema (PostgreSQL)
-   **AuditLogEntries**: `Id`, `Action`, `User`, `Resource`, `Details`, `Timestamp`.
-   **BlacklistEntries**: `Id`, `IpCidr`, `Reason`, `ExpiresAt`, `AddedBy`.
-   **WhitelistEntries**: `Id`, `RouteId`, `IpCidr`, `Reason`, `ExpiresAt`, `AddedBy`.

## Security Features

### Whitelist / Blacklist
-   **Database-First**: Source of truth is PostgreSQL.
-   **Sync**: Backend automatically syncs valid (non-expired) IPs to APISIX plugins.
-   **Metadata**: Entries include "Reason" and "Expiration" for auditability.

### PII Masking
-   **Dynamic Rules**: Regex-based masking configured per route.
-   **Observability**: Real-time metrics on masked fields.
