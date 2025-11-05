# RestSharp to HttpClient Migration Design

## Overview

We're migrating the LEAN engine from RestSharp to .NET's built-in HttpClient. RestSharp 106.12.0 is from 2021, no longer actively maintained, and we're ready to move to a more modern, well-supported solution.

The migration uses a two-phase abstraction-first approach that significantly reduces risk and makes the actual migration trivial.

**GitHub Issue:** [#8603 - Replace RestSharp For HttpClient](https://github.com/QuantConnect/Lean/issues/8603)

---

## Why This Migration Matters

### Technical Benefits
- Remove outdated dependency (RestSharp 106.12.0 from 2021)
- Use actively maintained .NET HttpClient
- Better performance through connection pooling
- Lower memory usage
- Improved async/await patterns

### Testing Benefits
- Current coverage is only ~30% for RestSharp components
- Two critical components have 0% test coverage
- Most tests require real API credentials (can't run in CI/CD)
- This migration will bring coverage to >80%

---

## The Problem: Why This Is Risky

The current state presents several challenges:

### Critical Components Affected
- **ApiConnection.cs** (169 lines) - Handles authentication with complex time-based hash
- **Api.cs** (2000+ lines) - Contains 50+ API endpoints
- **RestSubscriptionStreamReader.cs** (113 lines) - **0% test coverage**
- **OAuthTokenHandler.cs** (101 lines) - **0% test coverage**
- **BaseWebsocketsBrokerage.cs** (~500 lines) - Used by live trading brokerages

### What Could Go Wrong

**Authentication Breaking:**
- Time-based hash mechanism is sensitive and complex
- 7000-second expiration boundary needs careful handling
- Thread safety concerns with concurrent requests
- Any failure means all API calls fail

**Live Trading Impact:**
- OAuthTokenHandler manages authentication for live brokerages
- Failures could prevent order placement
- Financial impact for users trading real money

**Data Feed Failures:**
- RestSubscriptionStreamReader has zero tests
- Uses sync-over-async pattern that's risky to migrate
- Silent failures could break backtests

**Without proper testing, we have no automated way to verify the migration didn't break anything.**

---

## The Solution: Two-Phase Abstraction-First Approach

Instead of directly swapping RestSharp for HttpClient (risky!), we're doing this in two phases.

### Phase 1: Build the Abstraction + Comprehensive Tests

Create an abstraction layer that decouples our code from RestSharp, then write comprehensive tests against that abstraction.

**Key Innovation:** The tests will use the abstraction, not RestSharp directly. This means when we swap to HttpClient in Phase 2, the tests don't need to change at all.

**What We Build:**

1. **IHttpService Interface** - Clean abstraction for HTTP communication
2. **RestSharpHttpService** - Wrapper that implements IHttpService using current RestSharp code
3. **MockHttpService** - Test implementation that lets us control responses
4. **85+ Comprehensive Tests** - Cover all authentication, error handling, serialization, and async scenarios

**Production Code Changes:**
- Update ApiConnection to use IHttpService (add internal test constructor for DI)
- Update RestSubscriptionStreamReader to use IHttpService (add internal test constructor for DI)
- Public APIs remain completely unchanged - 100% backwards compatible

**Why This Works:**
- Tests validate the abstraction, not the implementation
- We can verify current RestSharp behavior is correct
- We build confidence before changing anything critical
- Phase 2 becomes trivial because the abstraction is already proven

### Phase 2: Simple Implementation Swap

With the abstraction in place and fully tested, Phase 2 is just swapping implementations.

**What We Do:**
1. Create `HttpClientService` - implements IHttpService using HttpClient
2. Find/replace: `new RestSharpHttpService()` → `new HttpClientService()`
3. Remove RestSharp package references
4. Done!

**What Makes This Safe:**
- All 85+ tests from Phase 1 still pass (they use IHttpService, not RestSharp!)
- Tests require ZERO changes
- Much smaller code review (just the new HttpClient implementation)
- Easy rollback if needed (just revert to RestSharpHttpService)

---

## Phase 1 Details: Abstraction & Testing

### The Abstraction Layer

**IHttpService Interface:**
- `SendAsync<T>()` - async HTTP requests
- `Send<T>()` - sync wrapper for interfaces that require it
- Works with `HttpServiceRequest` and `HttpServiceResponse<T>` types

**RestSharpHttpService:**
- Wraps existing RestClient functionality
- Implements IHttpService interface
- Maintains exact same behavior as current code
- Will be replaced in Phase 2

**MockHttpService (for tests):**
- Direct IHttpService implementation
- Allows configuring responses for any endpoint
- Captures requests for verification
- Never changes between phases

### Test Infrastructure

We're building reusable infrastructure to make testing clean and maintainable:

**Test Builders:**
- `HttpResponseBuilder` - fluent API for building test responses
- Response builders for each API type (Project, Backtest, Authentication, etc.)
- Makes tests readable and eliminates duplication

**Base Test Class:**
- `BaseApiTest` - common setup for all tests
- Creates MockHttpService automatically
- Helper methods for creating test instances
- Common constants and utilities

**Assertion Helpers:**
- Extension methods for common assertions
- `ShouldBeSuccessful()`, `ShouldBeFailed()`, etc.
- Makes test assertions clear and consistent

**Time Provider:**
- `MockTimeProvider` - control time in tests
- Critical for testing time-based authentication
- Test expiration boundaries and edge cases

### Test Coverage Plan

**ApiConnection Tests (40% → 90%+):**
- Authentication: hash generation, 7000-second refresh, caching, thread safety
- Error handling: all HTTP status codes (400-503), network failures, timeouts
- Serialization: JSON with custom converters, malformed responses, large payloads
- Async behavior: ConfigureAwait, exception propagation, cancellation, concurrent requests

**RestSubscriptionStreamReader Tests (0% → 100%):**
- ReadLine behavior in live vs backtest mode
- Custom header handling
- Rate limiting behavior
- Error and exception handling
- Large response and encoding tests
- All property behaviors (EndOfStream, ShouldBeRateLimited, etc.)

**OAuthTokenHandler Tests (0% → 100%):**
- Token caching until expiration
- Token refresh scenarios
- Thread safety with concurrent requests
- Correct endpoint and body format
- Error handling and exceptions

**Api.cs Endpoint Tests (~20% → ~60%):**
- Focus on 10 most critical endpoints
- Verify correct request format for each
- Test with both success and error scenarios

**Baseline Documentation Tests:**
- Capture exact RestSharp behavior for comparison
- Document authentication header format
- Document error handling behavior
- Use as reference during Phase 2

### Total Test Count: 85+ new tests

| Component | Before | After | Tests Added |
|-----------|--------|-------|-------------|
| ApiConnection | ~40% | >90% | 35+ tests |
| RestSubscriptionStreamReader | 0% | 100% | 20+ tests |
| OAuthTokenHandler | 0% | 100% | 18+ tests |
| Api.cs endpoints | ~20% | ~60% | 12+ tests |

### Backwards Compatibility

All Phase 1 changes are 100% backwards compatible:
- Public APIs completely unchanged
- Internal abstraction is an implementation detail
- RestSharp wrapper maintains exact same behavior
- New internal constructors only for testing (InternalsVisibleTo attribute)
- Deprecated `RestClient Client` property kept for compatibility

### Success Criteria for Phase 1

**Must Have:**
- Test coverage >80% for all RestSharp components
- All 85+ new tests pass
- All existing tests still pass
- Test suite runs in <5 minutes
- No tests require external API credentials
- Baseline behavior documented
- Code review approved

**Deliverables:**
- IHttpService abstraction fully implemented
- RestSharpHttpService wrapper complete
- MockHttpService test implementation
- All test infrastructure (builders, base classes, helpers)
- 85+ comprehensive tests
- Documentation (how to run tests, how to add tests, architecture decisions)

---

## Phase 2 Details: Migration to HttpClient

### What Actually Changes

**New Components:**

**LeanAuthenticationHandler** - DelegatingHandler that:
- Generates time-based authentication hash
- Caches authenticator for 7000 seconds (same as current)
- Thread-safe with proper locking
- Adds Authorization header (Basic auth with Base64)
- Adds Timestamp header

**HttpClientService** - IHttpService implementation that:
- Uses HttpClient instead of RestSharp
- Builds HttpRequestMessage from HttpServiceRequest
- Processes HttpResponseMessage into HttpServiceResponse
- Handles timeouts, network errors, cancellation
- Proper exception logging and error handling

### Migration Pattern

For each component using RestSharp:

**ApiConnection:**
- Create LeanAuthenticationHandler with userId/token
- Create HttpClient with the auth handler
- Replace `new RestSharpHttpService()` with `new HttpClientService(httpClient)`

**RestSubscriptionStreamReader:**
- Create HttpClient with appropriate timeout
- Add any custom headers
- Replace `new RestSharpHttpService()` with `new HttpClientService(httpClient)`

**Api.cs:**
- Add helper methods: `Post<T>(endpoint, payload)` and `Get<T>(endpoint)`
- Migrate 50+ endpoints to use helper methods
- Reduces code by ~60% per endpoint
- Maintain exact same public API

**OAuthTokenHandler:**
- No changes needed! Already uses ApiConnection which uses IHttpService
- This is the power of the abstraction

**BaseWebsocketsBrokerage:**
- Only update if it uses HTTP directly
- If WebSockets only, no changes needed

### Cleanup

- Remove RestSharp package references from all 5 .csproj files
- Remove all `using RestSharp` statements
- Verify build succeeds with zero warnings

### Validation

**Testing:**
- All 85+ Phase 1 tests must pass (ZERO test changes!)
- All existing tests must pass
- Run integration tests with real API (if credentials available)
- Performance tests: latency within 5% of baseline
- Load test: 1000 concurrent requests succeed

**Performance Metrics:**
- Average latency: <= baseline * 1.05
- P50, P95 latency: equal or better
- Memory usage: equal or better
- Throughput: equal or better

**Manual Testing:**
- Create project, upload file, run backtest
- Read results, create live algorithm
- Download data, read logs
- Verify all workflows work correctly

### Success Criteria for Phase 2

**Must Have:**
- All Phase 1 tests pass without modification
- HttpClientService fully implemented
- All components migrated to HttpClient
- RestSharp completely removed
- All existing tests pass
- Performance acceptable (within 5% of baseline)
- Integration tests pass
- Migration guide written
- Code review approved

### Rollback Plan

If critical issues are found after deployment:

1. Immediately revert PR #2
2. Redeploy previous version
3. Notify affected users
4. Perform root cause analysis
5. Fix and re-validate before re-attempting

The abstraction layer makes rollback easy - just switch back to RestSharpHttpService.

---

## Why This Approach Reduces Risk

**Traditional Approach (High Risk):**
- Write new HttpClient code
- Hope it works the same as RestSharp
- Test manually
- Deploy and pray
- Fix bugs in production

**Our Approach (Low Risk):**
- Create abstraction that both implementations share
- Write comprehensive tests against the abstraction
- Verify current behavior is correct
- Implement HttpClient version
- Tests automatically verify it works correctly
- Deploy with confidence

**Key Benefits:**
- Tests written once, used twice (RestSharp and HttpClient)
- Can validate each implementation independently
- Easier code review (smaller, focused PRs)
- Clear rollback path
- Better architecture for future changes

---

## Suggested Monitoring Post-Deployment

**First 24 Hours (Critical Window):**
- API success rate (should be >= 99.9%)
- API latency (P50, P95, P99)
- Authentication failures
- Error rates by endpoint
- Memory usage and GC pressure

**Alert Thresholds:**
- Critical: API success <95%, auth success <99%, latency >2x baseline
- Warning: API success <99%, P95 latency >1.5x baseline, error rate >20% increase

**Validation Period:**
- Week 1: Daily metric review
- Weeks 2-4: Weekly review
- Declare success after stable period with no critical issues

---

## Conclusion

This migration eliminates an outdated dependency while significantly improving our test coverage. The abstraction-first approach turns a risky migration into a safe, methodical process with clear validation at each step.

**Phase 1** builds the foundation with comprehensive tests.
**Phase 2** becomes trivial because the abstraction is already proven.

The result is a more maintainable, better-tested codebase with modern HTTP client usage.
