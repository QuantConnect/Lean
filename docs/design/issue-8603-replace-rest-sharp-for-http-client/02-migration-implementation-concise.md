# Phase 2: Migration Implementation

**Pull Request #2: "Migrate from RestSharp to HttpClient"**

## Overview

**Objective:** Replace RestSharp wrapper with HttpClient implementation while ensuring all Phase 1 tests continue to pass.

**Branch:** `feature/replace-restsharp-with-httpclient`
**Prerequisite:** Phase 1 PR must be merged and all tests passing

## Key Principle

**All 85+ Phase 1 tests must pass without ANY modification!** The tests use `IHttpService` abstraction created in Phase 1, so they work identically with both RestSharp and HttpClient implementations.

## Why Phase 2 is Trivial

Phase 1 already created:
- `IHttpService` abstraction
- `RestSharpHttpService` wrapper (current implementation)
- All production code using `IHttpService`
- All tests using `MockHttpService` (direct IHttpService implementation)

Phase 2 only needs to:
1. Create `HttpClientService` (new IHttpService implementation)
2. Swap: `new RestSharpHttpService()` → `new HttpClientService()`
3. Remove RestSharp packages
4. Done! Tests work unchanged.

---

## Task Breakdown

### 2.1: Verify Test Infrastructure Works (No Changes Needed!)

#### 2.1.1: Verify MockHttpService Works

**File:** `Tests/Common/Mocks/MockHttpService.cs` (Already created in Phase 1!)

**Purpose:** Verify that `MockHttpService` created in Phase 1 works with the new HttpClient implementation.

**What to Verify:**
- Run all Phase 1 tests - they should all still pass
- Expected: All 85+ tests pass without ANY changes!

**Key Insight:**
- `MockHttpService` implements `IHttpService` directly
- Tests use `IHttpService` abstraction (not RestSharp, not HttpClient!)
- Therefore, tests work with ANY IHttpService implementation
- No mock factory pattern needed!

**Why This is Better Than Factory Pattern:**
- Simpler - No factory abstraction needed
- Zero test changes - Tests never know about implementation details
- Better architecture - Tests depend on abstraction, not implementation
- Easier to maintain - Fewer moving parts

---

### 2.2: Implement HttpClient Service

#### 2.2.1: Implement HttpClientService

**Note:** `IHttpService` interface was already created in Phase 1! We just need to implement it using HttpClient.

**File:** `Common/HttpClientService.cs`

**Purpose:** HttpClient-based implementation of IHttpService.

**Key Responsibilities:**
- Implement `SendAsync<T>()` method using HttpClient
- Implement `Send<T>()` synchronous wrapper
- Build HttpRequestMessage from HttpServiceRequest
- Process HttpResponseMessage into HttpServiceResponse format
- Handle exceptions (TaskCanceledException, HttpRequestException, etc.)
- Proper disposal of resources

**Error Handling:**
- Catch `TaskCanceledException` for timeouts
- Catch `HttpRequestException` for network errors
- Log all errors appropriately
- Return HttpServiceResponse with error information

---

### 2.3: Update Production Code to Use HttpClient

**Strategy:** Simply swap `new RestSharpHttpService()` with `new HttpClientService()` everywhere. The abstraction handles everything else!

#### 2.3.1: Create LeanAuthenticationHandler

**File:** `Api/LeanAuthenticationHandler.cs` (new file)

**Purpose:** DelegatingHandler for time-based hash authentication. Replaces RestSharp's HttpBasicAuthenticator.

**Key Features:**
- Thread-safe with lock
- Reuses cached authenticator within 7000 seconds
- Same behavior as RestSharp version
- Adds both Authorization and Timestamp headers

**Responsibilities:**
- Generate time-based authentication hash
- Cache authenticator for 7000 seconds
- Add Authorization header (Basic auth with Base64)
- Add Timestamp header
- Thread-safe concurrent request handling

---

#### 2.3.2: Update ApiConnection

**File:** `Api/ApiConnection.cs`

**Change Required:** One-line swap from RestSharpHttpService to HttpClientService!

**Key Changes:**
- Create LeanAuthenticationHandler with userId and token
- Create HttpClient with authentication handler
- Set base address and timeout
- Pass HttpClient to new HttpClientService

**Everything else stays the same!** The `IHttpService` abstraction handles the rest.

---

#### 2.3.3: Run ApiConnection Tests

**Expected:** All 35+ tests pass without modification.

**If tests fail:** Debug and fix implementation, not the tests.

---

#### 2.3.4: Update RestSubscriptionStreamReader

**File:** `Engine/DataFeeds/Transport/RestSubscriptionStreamReader.cs`

**Change Required:** Replace RestSharpHttpService with HttpClientService

**Key Changes:**
- Create HttpClient with appropriate timeout
- Add headers to HttpClient.DefaultRequestHeaders if provided
- Create HttpClientService with the configured HttpClient

**Implementation Note:** Using `GetAwaiter().GetResult()` to maintain synchronous interface contract while using async HttpClient methods.

---

#### 2.3.5: Update OAuthTokenHandler

**File:** `Brokerages/Authentication/OAuthTokenHandler.cs`

**Change Required:** ZERO changes needed!

**Why?** OAuthTokenHandler already uses `ApiConnection`, which uses `IHttpService`. When we update ApiConnection to use HttpClient, OAuthTokenHandler automatically uses it too!

**This is the power of abstraction!**

---

### 2.4: Migrate Api.cs

#### 2.4.1: Create Helper Methods

**File:** `Api/Api.cs`

**Purpose:** Add helper methods to reduce duplication across 50+ endpoints

**Helper Methods:**
- `Post<TResponse>(endpoint, payload)` - POST requests with JSON body
- `Get<TResponse>(endpoint)` - GET requests
- Use shared `JsonSerializerSettings` with camelCase

**Benefits:**
- DRY principle - eliminate code duplication
- Consistent serialization across all endpoints
- Easier to maintain
- 60%+ code reduction per endpoint

---

#### 2.4.2: Migrate Endpoints

**Pattern for All 50+ Methods:**

**Migration Pattern:**
- Replace RestRequest creation with HttpServiceRequest
- Use helper methods (Post/Get) for common patterns
- Maintain exact same public API
- No breaking changes

**Code Reduction:**
- Old approach: ~18 lines per endpoint
- New approach: ~7 lines per endpoint
- Reduction: 61%+

---

#### 2.4.3: Migrate Special Cases

**Download Method (returns byte array):**
- Create HttpServiceRequest with POST method
- Serialize filePath and organizationId to JSON body
- Use ApiConnection.TryRequest
- Convert Base64 response to byte array

**DownloadBytes Method (custom headers):**
- Create HttpClient instance
- Create HttpRequestMessage with GET method
- Add custom headers
- Use HttpClient.SendAsync
- Read response as byte array

---

#### 2.4.4: Run Api.cs Tests

**Expected:** All 12+ endpoint tests pass.

---

### 2.5: Update BaseWebsocketsBrokerage

**Action:** Only update if BaseWebsocketsBrokerage uses HTTP directly. If it only uses WebSockets, no changes needed!

If updates are needed, follow the same pattern: swap `RestSharpHttpService` → `HttpClientService`.

---

### 2.6: Remove RestSharp Package References

#### 2.6.1: Update .csproj Files

Remove `<PackageReference Include="RestSharp" Version="106.12.0" />` from:

- `Api/QuantConnect.Api.csproj`
- `Brokerages/QuantConnect.Brokerages.csproj`
- `Engine/QuantConnect.Lean.Engine.csproj`
- `Messaging/QuantConnect.Messaging.csproj`
- `Tests/QuantConnect.Tests.csproj`

#### 2.6.2: Remove using Statements

Remove all `using RestSharp` statements from:
- Api/ApiConnection.cs
- Api/Api.cs
- Engine/DataFeeds/Transport/RestSubscriptionStreamReader.cs
- Brokerages/BaseWebsocketsBrokerage.cs
- Brokerages/Authentication/OAuthTokenHandler.cs

#### 2.6.3: Verify Build

**Command:** `dotnet build --configuration Release`

**Expected:** Build succeeds with zero errors, zero warnings about RestSharp.

---

### 2.7: Validation & Testing

#### 2.7.1: Run Full Test Suite

**Command:** `dotnet test --configuration Release`

**Expected:**
- All 85+ Phase 1 tests pass
- All existing integration tests pass
- Zero regressions

#### 2.7.2: Run Integration Tests (if credentials available)

**Command:** `dotnet test --filter "Category=Integration"`

**Expected:** All EXPLICIT integration tests pass with real API.

#### 2.7.3: Performance Testing

**Tests to Create:**
- Average latency for 100 requests
- Memory usage comparison
- Should be within 5% of baseline (or better)

**Metrics to Measure:**
- Average latency
- P50, P95, P99 latency
- Memory usage
- Requests per second

---

#### 2.7.4: Load Testing

**Test:** 1000 concurrent requests

**Expected:**
- All requests succeed
- No timeouts
- Performance within acceptable range

---

### 2.8: Documentation

#### 2.8.1: Update API Documentation

Update XML documentation comments in:
- ApiConnection.cs
- Api.cs
- IHttpService.cs
- HttpClientService.cs

#### 2.8.2: Create Migration Guide

**File:** `docs/RESTSHARP_MIGRATION_GUIDE.md`

**Sections:**
- Overview
- For End Users (no changes required)
- For Contributors (how to add new API endpoints)
- Testing (how tests work with mock factories)
- Benefits (performance, memory, maintenance)

---

## Deliverables Checklist

### Test Infrastructure (Already Done in Phase 1!)
- MockHttpService - Direct IHttpService implementation
- BaseApiTest uses MockHttpService
- All Phase 1 tests work with ANY IHttpService implementation
- Verify all Phase 1 tests pass (should be automatic!)

### Abstractions (Already Created in Phase 1!)
- `IHttpService` interface
- `RestSharpHttpService` wrapper (Phase 1)
- `HttpClientService` implementation (Phase 2 - NEW!)
- `LeanAuthenticationHandler` for auth (Phase 2 - NEW!)

### Components Migrated (Simple Swaps!)
- `ApiConnection` - Swap `RestSharpHttpService` → `HttpClientService`
- `Api.cs` - Uses ApiConnection (automatically updated!)
- `RestSubscriptionStreamReader` - Swap `RestSharpHttpService` → `HttpClientService`
- `OAuthTokenHandler` - Uses ApiConnection (automatically updated!)
- `BaseWebsocketsBrokerage` - Update if needed (may not use HTTP directly)

### RestSharp Removed
- All 5 .csproj references removed
- All using statements removed
- Build succeeds with zero warnings

### Testing Complete
- All 85+ Phase 1 tests pass
- All existing tests pass
- Integration tests pass
- Performance tests pass
- Load tests pass

### Documentation
- API documentation updated
- Migration guide created
- Code comments updated

---

## Success Criteria

**All Phase 1 tests pass (85+ tests, ZERO changes to test code!)**
All existing tests pass (zero regressions)
RestSharp completely removed (zero references)
Build succeeds with zero warnings
Performance within 5% of baseline (or better)
Load test: 1000 concurrent requests succeed
Integration tests pass (with real API)
Code review approved
CI/CD pipeline passes
Documentation complete

## Summary: Why Phase 2 is So Simple

**Phase 1 Created:**
- IHttpService abstraction
- RestSharpHttpService wrapper
- MockHttpService for tests
- All production code using IHttpService
- 85+ tests using MockHttpService

**Phase 2 Only Needs:**
- HttpClientService implementation
- Simple find/replace: `new RestSharpHttpService()` → `new HttpClientService()`
- Remove RestSharp packages

**Tests Don't Change Because:**
- They use IHttpService abstraction (not RestSharp!)
- MockHttpService implements IHttpService
- Abstraction validates behavior, not implementation

**This is the power of the abstraction-first approach!**

---

[Back to Pre-Migration Verification](./01-pre-migration-verification-concise.md) | [Back to Overview](./README-concise.md)
