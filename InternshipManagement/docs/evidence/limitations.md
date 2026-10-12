# Verification limitations

- Import performance was measured at the service/InMemory level with the same
  1,000- and 5,000-row CSV samples as the before run. It does not predict SQL
  Server or network performance.
- The production `StudentInternshipMgmtDb` database was not accessed. The
  `ForceAdminPasswordChange` migration was applied only to the dedicated
  `StudentInternshipMgmtDb_Test` database; the seeded flag was verified there.
- The upload size response is covered by a controller test, not a hosted HTTP
  multipart/API integration test.
- The JWT validator and token issuance are covered by unit/service tests. The
  deployment's new signing key, User Secrets setup, and production key rotation
  still need to be configured operationally. The previously exposed key must
  not be reused.
- Full `dotnet test` and Vitest suites completed. SQL/API throughput,
  production deployment configuration, and key rotation were not exercised.
