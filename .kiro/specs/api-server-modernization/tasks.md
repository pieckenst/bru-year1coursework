# Implementation Plan: API Server Modernization

## Overview

This implementation plan modernizes the existing TicketSalesApp.AdminServer (.NET 9.0) by adding WebSocket support, bulk export capabilities, enhanced security features, improved database abstraction, and comprehensive documentation while maintaining 100% backward compatibility.

## Tasks

- [x] 1. Set up Enhanced Project Structure and Dependencies
  - Add new NuGet packages for SignalR, Hangfire, Redis, MongoDB, WebAuthn, TOTP
  - Configure Docker Compose for development environment with Redis, MongoDB, Elasticsearch
  - Set up enhanced logging configuration with structured logging and correlation IDs
  - Configure OpenTelemetry for distributed tracing
  - _Requirements: 5.1, 6.3, 6.4, 8.1, 8.4_

- [ ]* 1.1 Write property test for dependency injection container
  - **Property 1: Dependency Resolution Consistency**
  - **Validates: Requirements 11.5**

- [x] 2. Implement WebSocket Support with SignalR
  - Create NotificationHub with authentication and group management
  - Implement INotificationService for real-time updates
  - Add WebSocket authentication using existing JWT tokens
  - Configure SignalR with Redis backplane for scaling
  - _Requirements: 1.1, 1.2, 1.4, 1.5, 1.6_

- [ ]* 2.1 Write property test for WebSocket authentication
  - **Property 1: WebSocket Authentication Enforcement**
  - **Validates: Requirements 1.1**

- [ ]* 2.2 Write property test for data change broadcasting
  - **Property 2: Data Change Broadcasting**
  - **Validates: Requirements 1.2**

- [ ]* 2.3 Write property test for concurrent connection management
  - **Property 4: Concurrent Connection Management**
  - **Validates: Requirements 1.4**

- [x] 3. Refactor Authentication System
  - Split large AuthController into focused controllers (Authentication, WindowsAuth, WebAuthn, TwoFactor)
  - Create separate development controller for debug endpoints
  - Implement service layer pattern for authentication business logic
  - Maintain existing JWT token format and Windows Authentication compatibility
  - _Requirements: 10.1, 10.2, 10.3, 11.1, 11.2, 11.3_

- [ ]* 3.1 Write property test for JWT token compatibility
  - **Property 19: JWT Token Format Compatibility**
  - **Validates: Requirements 10.2**

- [x] 4. Implement WebAuthn (FIDO2) Support
  - Add WebAuthnService using Fido2.AspNetCore library
  - Create WebAuthnController for registration and authentication endpoints
  - Implement WebAuthnCredential data model and Entity Framework configuration
  - Add WebAuthn management UI endpoints
  - _Requirements: 4.3_

- [ ]* 4.1 Write property test for WebAuthn authentication flow
  - **Property 10: WebAuthn Authentication Flow**
  - **Validates: Requirements 4.3**

- [x] 5. Implement Two-Factor Authentication (TOTP)
  - Add TotpService using OtpNet library
  - Create TwoFactorController for TOTP setup and validation
  - Implement TOTP secret storage and recovery codes
  - Add QR code generation for TOTP setup
  - _Requirements: 4.4_

- [ ]* 5.1 Write property test for TOTP code validation
  - **Property 11: TOTP Code Validation**
  - **Validates: Requirements 4.4**

- [x] 6. Implement Policy-Based Authorization (Fix Manual IsAdmin() Checks)
  - Create DatabaseRoleAuthorizationHandler to access user roles from database context (fixes ASP.NET Core policy limitation)
  - Support both legacy Role field (0=User, 1=Admin) and modern RBAC system (UserRoles, Roles, Permissions tables)
  - Implement caching for user roles and permissions to improve performance (15-minute cache)
  - Configure authorization policies (AdminOnly, CanManageBuses, CanViewReports, etc.) in Startup
  - Replace all manual IsAdmin() checks in controllers with [Authorize(Policy = "AdminOnly")] attributes
  - Add role cache invalidation service for when user roles change
  - Create permission-based authorization handler for fine-grained access control
  - _Requirements: 4.6_

- [ ]* 6.1 Write property test for authorization consistency
  - **Property 12: Policy-Based Authorization Consistency**
  - **Validates: Requirements 4.6**

- [x] 7. Implement Bulk Export System
  - Create ExportService with support for CSV, Excel, and JSON formats
  - Implement background job processing using Hangfire
  - Add streaming export for large datasets
  - Create export progress tracking with WebSocket notifications
  - Add export file management with expiration
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7_

- [ ]* 7.1 Write property test for multi-format export generation
  - **Property 6: Multi-Format Export Generation**
  - **Validates: Requirements 2.1, 2.2, 2.3**

- [ ]* 7.2 Write property test for streaming export memory efficiency
  - **Property 7: Streaming Export Memory Efficiency**
  - **Validates: Requirements 2.4**

- [ ]* 7.3 Write property test for export progress notification
  - **Property 8: Export Progress Notification**
  - **Validates: Requirements 2.5**

- [x] 8. Implement Enhanced Database Architecture
  - Create repository pattern interfaces and implementations
  - Implement Unit of Work pattern for transaction management
  - Add MongoDB support for document storage (logs, analytics)
  - Enhance existing Entity Framework configuration with query optimization
  - Add database health checks and connection monitoring
  - _Requirements: 6.1, 6.3, 6.6, 6.7, 11.4_

- [ ]* 8.1 Write property test for database provider compatibility
  - **Property 15: Database Provider Compatibility**
  - **Validates: Requirements 6.1**

- [ ]* 8.2 Write property test for MongoDB document storage
  - **Property 16: MongoDB Document Storage**
  - **Validates: Requirements 6.3**

- [x] 9. Implement Redis Caching System
  - Create ICacheService interface and Redis implementation
  - Add response caching for frequently accessed data (buses, routes, users)
  - Implement cache invalidation strategies with event-driven updates
  - Configure Redis for session state and SignalR backplane
  - _Requirements: 5.1, 5.6_

- [ ]* 9.1 Write property test for cache consistency
  - **Property 13: Cache Consistency and Invalidation**
  - **Validates: Requirements 5.1**

- [ ] 10. Implement Background Job Processing
  - Configure Hangfire for background job processing
  - Create background services for exports, notifications, and maintenance tasks
  - Implement job retry policies and error handling
  - Add job monitoring and management endpoints
  - _Requirements: 5.5_

- [ ]* 10.1 Write property test for background job processing
  - **Property 14: Background Job Processing**
  - **Validates: Requirements 5.5**

- [ ] 11. Enhance Monitoring and Observability
  - Configure OpenTelemetry for distributed tracing
  - Enhance existing Prometheus metrics with custom business metrics
  - Add structured logging with correlation IDs
  - Implement comprehensive health checks for all dependencies
  - Create Grafana dashboard templates
  - _Requirements: 8.1, 8.3, 8.4, 8.6_

- [ ]* 11.1 Write property test for distributed tracing
  - **Property 17: Distributed Tracing Propagation**
  - **Validates: Requirements 8.4**

- [ ] 12. Implement Enhanced Input Validation
  - Add FluentValidation for comprehensive input validation
  - Create custom validation rules for business logic
  - Implement request/response validation middleware
  - Add input sanitization and XSS prevention
  - _Requirements: 4.7_

- [ ] 13. Enhance API Documentation
  - Upgrade OpenAPI specification to 3.0+ with comprehensive examples
  - Add detailed endpoint documentation with request/response schemas
  - Create language-agnostic API specification for cross-platform implementation
  - Generate Postman collections for API testing
  - Document all business logic and integration patterns
  - _Requirements: 3.1, 3.2, 3.7, 9.1, 9.2, 12.6_

- [ ]* 13.1 Write property test for API specification completeness
  - **Property 20: OpenAPI Specification Completeness**
  - **Validates: Requirements 12.6**

- [ ] 14. Implement Comprehensive Error Handling
  - Create centralized exception handling middleware
  - Implement structured error responses with correlation IDs
  - Add WebSocket error handling for real-time scenarios
  - Create error handling for background jobs with retry policies
  - _Requirements: 11.7_

- [ ] 15. Add Security Enhancements
  - Implement enhanced rate limiting with per-user and per-endpoint limits
  - Add HSTS headers and security middleware
  - Implement account lockout for failed login attempts
  - Add comprehensive security event logging
  - _Requirements: 4.8, 4.10_

- [ ] 16. Create Comprehensive Documentation
  - Add XML documentation comments for all public APIs
  - Create architectural documentation with design patterns and decisions
  - Document all business rules and validation logic in language-neutral terms
  - Create database schema documentation in database-agnostic format
  - Write integration guides and developer onboarding documentation
  - _Requirements: 12.1, 12.2, 12.3, 12.4, 12.5, 12.9, 12.10_

- [ ] 17. Implement Backward Compatibility Testing
  - Create comprehensive test suite for existing API endpoints
  - Implement compatibility tests for JWT token format and claims
  - Add integration tests for Windows Authentication functionality
  - Test all existing request/response models and serialization
  - _Requirements: 10.1, 10.2, 10.3, 10.5_

- [ ]* 17.1 Write property test for backward compatibility
  - **Property 18: Backward Compatibility Preservation**
  - **Validates: Requirements 10.1**

- [ ] 18. Performance Optimization and Load Testing
  - Implement database query optimization with compiled queries
  - Add connection pooling optimization
  - Create performance benchmarks and load testing scenarios
  - Optimize memory usage and garbage collection
  - _Requirements: 5.2, 5.7_

- [ ] 19. Final Integration and Testing
  - Run comprehensive integration tests across all components
  - Perform end-to-end testing of WebSocket functionality
  - Test export system with large datasets
  - Validate security features (WebAuthn, TOTP, authorization)
  - Verify monitoring and observability features
  - _Requirements: All_

- [ ]* 19.1 Write integration tests for complete system functionality
  - Test WebSocket + Export + Authentication integration
  - Test database failover and recovery scenarios
  - Test horizontal scaling with multiple instances

- [ ] 20. Deployment and Documentation Finalization
  - Update Docker configuration for production deployment
  - Create deployment guides and configuration templates
  - Finalize API documentation and SDK generation
  - Create migration guides from existing system
  - Document all external dependencies and their purposes
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 12.8, 12.10_

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Property tests validate universal correctness properties
- Unit tests validate specific examples and edge cases
- Integration tests ensure end-to-end functionality
- All existing functionality must remain working throughout implementation