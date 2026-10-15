# Employee Training Portal

ASP.NET Core 10 MVC, EF Core / SQL Server, Entra ID SSO and IIS deployment.

Video counts are administrator-managed CRUD; there is no fixed four/five-video limit. Programs need at least one video before their quiz can unlock.

- [Demo on another laptop (no corporate SSO required)](docs/DEMO-ON-ANOTHER-LAPTOP.md)
- [Installation and upgrade](docs/INSTALLATION.md)
- [Administrator and employee manual](docs/USER-MANUAL.md)
- [Technical notes and requirement status](docs/TECHNICAL.md)
- [Validation and client acceptance](docs/TEST-REPORT.md)
- [Handover agenda](docs/HANDOVER.md)
- [Run locally with developer sign-in](docs/DEVELOPMENT-AUTHENTICATION.md) (requires `deployment/DevelopmentLoginAccounts.sql`)

Configuration deliberately contains no live tenant credentials. Development authorization bypass is disabled. Apply the reviewed database script and configure the corporate identity mapping before use.

