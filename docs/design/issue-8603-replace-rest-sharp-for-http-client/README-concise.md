# RestSharp to HttpClient Migration Plan

## Overview

This directory contains the complete design documentation for migrating the QuantConnect LEAN engine from RestSharp to HttpClient, addressing [Issue #8603](https://github.com/QuantConnect/Lean/issues/8603).

## Problem Statement

The LEAN engine currently uses RestSharp version 106.12.0 (released ~2021), which is outdated and no longer actively maintained. The .NET runtime's built-in `HttpClient` is more efficient, better maintained, and more aligned with modern .NET best practices.

## Solution Approach

This migration follows a **two-phase abstraction-first approach** prioritizing safety, maintainability, and architectural excellence:

1. **Phase 1: Abstraction & Verification** - Create IHttpService abstraction + comprehensive tests
2. **Phase 2: Simple Implementation Swap** - Replace RestSharp with HttpClient (trivial!)

**Key Innovation:** Phase 1 creates the abstraction layer **immediately**, making Phase 2 a simple implementation swap instead of a risky architectural refactor!

## Documentation Structure

### [00 - Risk Analysis](./00-risk-analysis-concise.md)
Comprehensive analysis of:
- Current RestSharp usage patterns
- Migration risks and mitigation strategies
- Current test coverage gaps
- Critical components requiring validation

**Read this first** to understand the scope and challenges.

### [01 - Abstraction & Verification (Phase 1)](./01-pre-migration-verification-concise.md)
**Phase 1 (Pull Request #1)**: **Abstraction-First Approach** - Create abstraction layer & comprehensive tests

**Production Code Changes (Moderate, 100% Backwards Compatible):**
- Create `IHttpService` interface - Clean HTTP service abstraction
- Create `RestSharpHttpService` - Wrapper implementing IHttpService using RestSharp
- Update `ApiConnection` to use IHttpService (internal test constructor added)
- Update `RestSubscriptionStreamReader` to use IHttpService (internal test constructor added)
- **All production code uses IHttpService from day one!**

**Test Infrastructure (Never Changes!):**
- `MockHttpService` - Direct IHttpService implementation (NOT factory pattern!)
- `BaseApiTest` - Uses MockHttpService (NEVER changes in Phase 2!)
- 85+ comprehensive tests - All using IHttpService abstraction
- Tests validate abstraction, not implementation details
- **Key benefit: Tests require ZERO changes in Phase 2!**

**Why This is Better:**
- Tests never depend on RestSharp types
- Abstraction validated by 85+ tests before migration
- Phase 2 becomes trivial (just swap implementations)
- SOLID principles from day one
- Lower overall risk

### [02 - Simple Implementation Swap (Phase 2)](./02-migration-implementation-concise.md)
**Phase 2 (Pull Request #2)**: Replace RestSharp with HttpClient - **Trivial Thanks to Abstraction!**

**What Phase 2 Actually Does:**
1. Create `HttpClientService` - New IHttpService implementation using HttpClient
2. Simple find/replace: `new RestSharpHttpService()` → `new HttpClientService()`
3. Remove RestSharp package references
4. Done! All 85+ tests work unchanged!

**What Phase 2 Does NOT Do:**
- NOT: Create abstractions (already done in Phase 1!)
- NOT: Update tests (they use IHttpService, which doesn't change!)
- NOT: Refactor architecture (architecture is already correct!)
- NOT: Change APIs (100% backwards compatible!)

**This is why the abstraction-first approach is superior!**


## Components Affected

### Core Components (5 files)
1. **Api/ApiConnection.cs** - API connection and authentication (Critical)
2. **Api/Api.cs** - 50+ API endpoint methods (High impact)
3. **Engine/DataFeeds/Transport/RestSubscriptionStreamReader.cs** - Data feed polling (0% test coverage)
4. **Brokerages/BaseWebsocketsBrokerage.cs** - Brokerage base class (High impact)
5. **Brokerages/Authentication/OAuthTokenHandler.cs** - OAuth token management (0% test coverage)

### Project Files (5 projects)
- QuantConnect.Api.csproj
- QuantConnect.Brokerages.csproj
- QuantConnect.Lean.Engine.csproj
- QuantConnect.Messaging.csproj
- QuantConnect.Tests.csproj

## Benefits of This Migration

### Technical Benefits
- Remove outdated dependency (RestSharp 106.12.0 from 2021)
- Use modern .NET HttpClient with active maintenance
- Better performance through connection pooling
- Lower memory usage
- Better integration with .NET ecosystem
- Improved async/await patterns

### Process Benefits
- Significantly improved test coverage (30% → 80%+)
- Components with 0% coverage reach 100%
- Robust test infrastructure for future changes
- Clear baseline behavior documentation
- Reduced risk through comprehensive testing

## Risk Level

| Without Tests | With Comprehensive Tests |
|---------------|--------------------------|
| **HIGH RISK** | **LOW RISK** |
| Silent failures possible | All failures detected |
| No regression detection | Comprehensive regression suite |
| Manual testing required | Automated validation |
| Uncertain behavior | Documented baseline |

## Getting Started

1. **Review the risk analysis**: [00-risk-analysis-concise.md](./00-risk-analysis-concise.md)
2. **Understand Phase 1**: [01-pre-migration-verification-concise.md](./01-pre-migration-verification-concise.md)
3. **Plan Phase 2**: [02-migration-implementation-concise.md](./02-migration-implementation-concise.md)

## Status

- Phase 1: Pre-Migration Verification (Not Started)
- Phase 2: Migration Implementation (Not Started)

## References

- **GitHub Issue**: [#8603 - Replace RestSharp For HttpClient](https://github.com/QuantConnect/Lean/issues/8603)
- **RestSharp Version**: 106.12.0
- **Target**: .NET HttpClient (built-in)

