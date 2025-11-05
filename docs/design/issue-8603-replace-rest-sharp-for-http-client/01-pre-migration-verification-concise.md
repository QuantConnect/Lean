# Phase 1: Pre-Migration Verification

**Pull Request #1: "Add comprehensive unit tests for RestSharp components in preparation for HttpClient migration"**

## Overview

**Objective:** Build a robust, maintainable test suite that validates current RestSharp behavior and will validate future HttpClient behavior with **minimal changes**.

**Branch:** `test/restsharp-comprehensive-coverage`
**Target Coverage:** >80% for all RestSharp-dependent components

## Production Code Changes (Moderate, 100% Backwards Compatible)

Phase 1 creates an **HTTP service abstraction** to decouple from RestSharp, enabling clean testing and simpler Phase 2 migration:

### Design Philosophy

Instead of testing against RestSharp directly, we create a clean abstraction (`IHttpService`) and wrap RestSharp in Phase 1. This means:
- Tests never depend on RestSharp types
- Abstraction is validated by comprehensive tests
- Phase 2 becomes trivial (just swap implementation)
- Better architecture following SOLID principles
- Lower overall risk

### Required Changes

#### 1. Create HTTP Service Abstraction

**File:** `Common/Interfaces/IHttpService.cs` (new file)

**Purpose:** Define abstraction for HTTP communication that enables testing and decouples from specific HTTP client implementation.

**Key Components:**
- `IHttpService` interface with `SendAsync<T>()` and `Send<T>()` methods
- `HttpServiceRequest` class - request abstraction
- `HttpServiceResponse<T>` class - response abstraction

#### 2. Create RestSharp Wrapper Implementation

**File:** `Common/RestSharpHttpService.cs` (new file)

**Purpose:** IHttpService implementation using RestSharp. This wrapper will be replaced with HttpClient in Phase 2.

**Key Responsibilities:**
- Wrap existing RestClient functionality
- Implement IHttpService interface
- Handle request building from HttpServiceRequest
- Process RestSharp responses into HttpServiceResponse format
- Maintain exact same behavior as current implementation

#### 3. Update ApiConnection to Use IHttpService

**File:** `Api/ApiConnection.cs`

**Changes:**
- Replace direct RestClient usage with IHttpService
- Add internal constructor for test dependency injection
- Maintain backward compatibility with existing public API
- Keep deprecated `RestClient Client` property for compatibility

**Key Points:**
- Public API remains unchanged
- Authentication logic preserved
- All existing functionality maintained
- Internal test constructor allows mock injection

#### 4. Update RestSubscriptionStreamReader to Use IHttpService

**File:** `Engine/DataFeeds/Transport/RestSubscriptionStreamReader.cs`

**Changes:**
- Replace RestClient with IHttpService
- Add internal constructor for testing
- Maintain IStreamReader interface contract
- Preserve all existing behavior

#### 5. Add InternalsVisibleTo Attribute

**Files:** Project files or AssemblyInfo

**Purpose:** Allow test assembly to access internal constructors for dependency injection

### Backwards Compatibility Guarantee

These changes are **100% backwards compatible** because:

1. **Public API unchanged** - All existing public constructors work identically
2. **Same behavior** - RestSharp wrapper maintains exact same functionality
3. **Internal abstraction** - IHttpService is internal implementation detail
4. **No breaking changes** - All client code continues to work
5. **Deprecated property** - `RestClient Client` property kept for compatibility (marked obsolete)
6. **Additive only** - New classes added, existing code behavior unchanged

### Components Not Requiring Changes

- **OAuthTokenHandler** - Already uses dependency injection (takes `ApiConnection` as parameter)
- **Api.cs** - Already uses `ApiConnection` (will automatically use new abstraction)
- **BaseWebsocketsBrokerage** - Will be addressed in Phase 2

---

## Task Breakdown

### 1.1: Create Test Infrastructure & Shared Utilities

#### 1.1.1: HTTP Response Builder (Builder Pattern)

**File:** `Tests/Common/Builders/HttpResponseBuilder.cs`

**Purpose:** Fluent builder for creating test HTTP responses that abstracts HTTP client implementation.

**Key Features:**
- Fluent API for building responses
- Support for status codes, content, headers, errors
- Support for JSON serialization
- Works with `HttpServiceResponse` abstraction (not tied to RestSharp or HttpClient)

**Responsibilities:**
- Build mock HTTP responses for testing
- Support various response scenarios (success, errors, timeouts)
- Enable easy test data construction

---

#### 1.1.2: Mock HTTP Service Implementation

**File:** `Tests/Common/Mocks/MockHttpService.cs`

**Purpose:** Mock implementation of IHttpService that allows complete control over responses for testing.

**Key Features:**
- Direct implementation of `IHttpService` (not a factory!)
- Request capture and verification
- Sequential response support
- Error injection
- **Same implementation in Phase 1 and Phase 2** - never changes!

**Why This Approach is Better:**
- No factory pattern needed - Direct IHttpService implementation
- Tests never change - Same MockHttpService in Phase 1 and Phase 2
- Simpler architecture - Fewer abstractions to maintain
- Better testability - Tests use the actual abstraction, not a factory

---

#### 1.1.3: Test Data Builders (Builder Pattern)

**File:** `Tests/Common/Builders/ApiResponseBuilders.cs`

**Purpose:** Fluent builders for creating test API response objects. Eliminates duplication of test data setup.

**Builders to Create:**
- `ProjectResponseBuilder`
- `AuthenticationResponseBuilder`
- `BacktestResponseBuilder`
- `LiveAlgorithmResponseBuilder`
- `TokenResponseBuilder`
- `CompileResponseBuilder`
- `FileResponseBuilder`

**Benefits:**
- DRY principle - eliminate duplicate test data setup
- Fluent API makes tests readable
- Easy to create variations for different test scenarios
- Maintainable - changes to response structure happen in one place

---

#### 1.1.4: Base Test Class (Template Method Pattern)

**File:** `Tests/Common/BaseApiTest.cs`

**Purpose:** Provides common setup/teardown and utilities for all API tests.

**Key Features:**
- Sets up `MockHttpService` in base Setup() method
- Provides helper methods for creating test instances
- Common constants (TestUserId, TestToken)
- Helper methods for authentication verification
- Mock time provider creation

**DRY WIN:** All test classes inherit this. **This code NEVER changes between Phase 1 and Phase 2!**

**Key Benefits:**
- Zero changes in Phase 2 - Tests use IHttpService abstraction from day one
- Simpler setup - No factory pattern to manage
- Direct dependency injection - Uses internal test constructors
- Same tests work with both RestSharp and HttpClient - They only know about IHttpService

---

#### 1.1.5: Assertion Helpers (Extension Methods)

**File:** `Tests/Common/Extensions/AssertionExtensions.cs`

**Purpose:** Custom assertions that make tests more readable and maintainable.

**Key Methods:**
- `ShouldBeSuccessful()` - Assert request succeeded
- `ShouldBeFailed()` - Assert request failed
- `ShouldHaveError()` - Assert specific error message
- `ShouldBeAuthenticationHash()` - Verify auth hash
- `ShouldHaveTimestamp()` - Verify timestamp

**Benefits:**
- More readable test assertions
- Consistent error messages
- Reusable across all test classes

---

#### 1.1.6: Time Provider Abstraction

**File:** `Tests/Common/Mocks/MockTimeProvider.cs`

**Purpose:** Mock time for testing time-based authentication logic.

**Features:**
- Control current time in tests
- Advance time programmatically
- Test time-based expiration logic
- Test boundary conditions

---

### 1.2: ApiConnection Comprehensive Unit Tests

#### Test Organization

Separate test fixtures by concern (Single Responsibility Principle):

1. **ApiConnectionAuthenticationTests** - Authentication logic
2. **ApiConnectionErrorHandlingTests** - Error scenarios
3. **ApiConnectionSerializationTests** - JSON handling
4. **ApiConnectionAsyncTests** - Async/await patterns

---

#### 1.2.1: Authentication Tests

**File:** `Tests/Api/ApiConnectionAuthenticationTests.cs`

**Test Coverage:**
- Correct hash generation with valid credentials
- Authentication refresh after 7000 seconds
- Cached authenticator reuse within 7000 seconds
- Timestamp header addition
- Basic Authorization header format
- Thread safety with concurrent requests
- Connected property behavior (true when authenticated, false otherwise)

**Tests to Write:** 8-10 test methods
**Coverage Target:** >95% of authentication logic

---

#### 1.2.2: Error Handling Tests

**File:** `Tests/Api/ApiConnectionErrorHandlingTests.cs`

**Test Coverage:**
- HTTP error status codes (400, 401, 403, 404, 429, 500, 503)
- Network timeout scenarios
- Connection refused errors
- DNS failures
- SSL/TLS errors
- Error logging verification

**Tests to Write:** 10-12 test methods
**Coverage Target:** 100% of error handling paths

---

#### 1.2.3: Serialization Tests

**File:** `Tests/Api/ApiConnectionSerializationTests.cs`

**Test Coverage:**
- JSON deserialization with custom converters
- Malformed JSON handling
- Empty response handling
- Null field handling
- Large response handling (>1MB)
- Special characters in JSON

**Tests to Write:** 6-8 test methods
**Coverage Target:** 100% of serialization paths

---

#### 1.2.4: Async Behavior Tests

**File:** `Tests/Api/ApiConnectionAsyncTests.cs`

**Test Coverage:**
- ConfigureAwait(false) behavior
- Synchronous wrapper (no deadlock)
- Exception propagation
- Cancellation token support
- Concurrent request handling

**Tests to Write:** 5-6 test methods
**Coverage Target:** 100% of async patterns

---

### 1.3: RestSubscriptionStreamReader Comprehensive Tests

**File:** `Tests/Engine/DataFeeds/Transport/RestSubscriptionStreamReaderTests.cs`

**Current Coverage:** **0%** → **Target: 100%**

**Test Coverage:**
- ReadLine() returns content on successful GET
- Custom headers added correctly
- ShouldBeRateLimited behavior (true in live mode, false in backtest)
- EndOfStream behavior (different in live vs backtest mode)
- Error handling (returns empty string on HTTP error)
- Exception handling
- Large response handling
- Empty response handling
- UTF-8 encoding
- TransportMedium property returns Rest
- StreamReader property returns null
- Disposal doesn't throw

**Tests to Write:** 18-20 test methods
**Coverage Target:** 100%

---

### 1.4: OAuthTokenHandler Comprehensive Tests

**File:** `Tests/Brokerages/Authentication/OAuthTokenHandlerTests.cs`

**Current Coverage:** **0%** → **Target: 100%**

**Test Coverage:**
- Successful token retrieval
- Token caching until expiration
- Token refresh after expiration
- Token refresh on expiration boundary
- Exception thrown on failed request
- Exception thrown on empty access token
- Thread safety with concurrent requests
- Correct endpoint usage ("live/auth0/refresh")
- Correct JSON body serialization
- Return value format (TokenType + AccessToken)
- Cancellation handling
- ApiConnection failure handling

**Tests to Write:** 15-18 test methods
**Coverage Target:** 100%

---

### 1.5: Api.cs Critical Endpoint Tests

**File:** `Tests/Api/ApiEndpointTests.cs`

**Focus:** 10 most critical endpoints

**Test Coverage:**
- CreateProject - correct request format
- ReadProject - correct request format
- CreateBacktest - correct request format
- ReadBacktest - correct request format
- CreateLiveAlgorithm - correct request format
- StopLiveAlgorithm - correct request format
- ReadLiveLogs - correct request format
- Download - successful download handling
- DownloadBytes - custom headers included
- Authenticate - credential validation

**Tests to Write:** 10-12 test methods
**Coverage Target:** 60%+ of critical endpoints

---

### 1.6: Baseline Behavior Documentation Tests

**File:** `Tests/Api/RestSharpBaselineBehaviorTests.cs`

**Purpose:** Document exact RestSharp behavior for validation during Phase 2 migration.

**Test Coverage:**
- Authentication header format
- Timestamp header format
- Request body format
- Error exception behavior
- IsSuccessful property behavior
- Content encoding behavior

**Tests to Write:** 6-8 documentation tests
**Output:** Baseline behavior report for Phase 2 validation

---

## Deliverables Checklist

### Production Code Changes (Moderate, 100% Backwards Compatible)

- Create `IHttpService` interface and related DTOs (`Common/Interfaces/IHttpService.cs`)
- Create `RestSharpHttpService` wrapper (`Common/RestSharpHttpService.cs`)
- Update `ApiConnection.cs` to use `IHttpService`
- Update `RestSubscriptionStreamReader.cs` to use `IHttpService`
- Add `InternalsVisibleTo` attribute for test assembly access
- Add internal test constructors for dependency injection
- Verify public API unchanged (100% backwards compatible)

### Test Infrastructure

- `HttpResponseBuilder` with fluent API (builds `HttpServiceResponse`)
- `MockHttpService` - Direct implementation of `IHttpService` (NOT a factory!)
- Response builders (5+ types: Project, Authentication, Backtest, etc.)
- `BaseApiTest` base class (uses MockHttpService, NEVER changes between phases!)
- Assertion extension methods
- `MockTimeProvider` utility
- Request capture/verification utilities
- **Key Benefit:** Test infrastructure NEVER changes in Phase 2!

### Test Files

- `ApiConnectionAuthenticationTests.cs` (8-10 tests)
- `ApiConnectionErrorHandlingTests.cs` (10-12 tests)
- `ApiConnectionSerializationTests.cs` (6-8 tests)
- `ApiConnectionAsyncTests.cs` (5-6 tests)
- `RestSubscriptionStreamReaderTests.cs` (18-20 tests)
- `OAuthTokenHandlerTests.cs` (15-18 tests)
- `ApiEndpointTests.cs` (10-12 tests)
- `RestSharpBaselineBehaviorTests.cs` (6-8 tests)

### Documentation

- Test infrastructure README
- How to run tests guide
- How to add new tests guide
- Architecture decisions document
- Baseline behavior report

---

## Success Criteria

### Coverage Targets

| Component | Before | Target | Tests Added |
|-----------|--------|--------|-------------|
| ApiConnection | ~40% | >90% | 35+ tests |
| RestSubscriptionStreamReader | 0% | 100% | 20+ tests |
| OAuthTokenHandler | 0% | 100% | 18+ tests |
| Api.cs critical endpoints | ~20% | ~60% | 12+ tests |
| **Total** | **~30%** | **>80%** | **85+ tests** |

### Quality Metrics

- All existing tests still pass (100% success rate)
- All new tests pass (100% success rate)
- Test suite completes in <5 minutes
- No flaky tests detected (run 10 times, all pass)
- Code coverage reports generated
- All tests can run without external dependencies
- No tests marked EXPLICIT

### Code Quality

- Code review approved
- Clear test names (readable as specifications)
- Comprehensive comments
- Proper error messages in assertions

---

## Pull Request Summary

### Overview
This PR adds comprehensive unit test coverage for all components using RestSharp, in preparation for migrating from RestSharp to HttpClient (Issue #8603).

### Motivation
- Current test coverage is insufficient (~30%)
- RestSubscriptionStreamReader has 0% coverage
- OAuthTokenHandler has 0% coverage
- Most existing tests require real API access (marked EXPLICIT)
- Need confidence before refactoring critical infrastructure

### Changes

#### Production Code Changes (Moderate, 100% Backwards Compatible)
- Created `IHttpService` abstraction layer (interface + DTOs)
- Created `RestSharpHttpService` wrapper (implements IHttpService using RestSharp)
- Updated `ApiConnection.cs` to use `IHttpService`
- Updated `RestSubscriptionStreamReader.cs` to use `IHttpService`
- Added `InternalsVisibleTo` attribute for test assembly access
- Added internal test constructors for dependency injection
- **Zero breaking changes** - all public APIs unchanged

#### Test Infrastructure & Tests
- Created reusable test infrastructure
- Added 35+ tests for ApiConnection (40% → 90%+ coverage)
- Added 20+ tests for RestSubscriptionStreamReader (0% → 100% coverage)
- Added 18+ tests for OAuthTokenHandler (0% → 100% coverage)
- Added 12+ tests for critical Api.cs endpoints
- Created baseline behavior documentation tests
- All tests use mocks - no real API access required

### Test Infrastructure Design
The test infrastructure is designed to **completely eliminate changes** during Phase 2 migration:
- `MockHttpService` directly implements `IHttpService` (tests use the abstraction!)
- `HttpResponseBuilder` uses builder pattern for test data
- `BaseApiTest` provides common setup/teardown (NEVER changes!)
- **Phase 2 requires ZERO test infrastructure changes** - Tests already use IHttpService abstraction!

### Coverage Impact
| Component | Before | After |
|-----------|--------|-------|
| ApiConnection | ~40% | >90% |
| RestSubscriptionStreamReader | 0% | 100% |
| OAuthTokenHandler | 0% | 100% |
| Api.cs (critical endpoints) | ~20% | ~60% |

### Next Steps
Phase 2 (separate PR): Migrate from RestSharp to HttpClient using this test suite for validation.

---

[Back to Risk Analysis](./00-risk-analysis-concise.md) | [Next: Migration Implementation](./02-migration-implementation-concise.md)
