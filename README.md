# StayBnb

GitHub Repo: https://github.com/BlakeMath/StayBnb
Dear Marker, please look at commit history for proof of developement.

# Features
- Guest registration and role-based dashboards.
- Host applications and Admin approval.
- Property browsing, booking requests and price calculation.
- Guest document uploads and Host verification.
- Arrival and departure tracking.
- Internal messaging, replies and notifications.
- Activity logging.

# Requirements
- Visual Studio 2022 with ASP.NET installed
- .NET 8 SDK
- SQL Server Express LocalDB

# Setup
1. Open StayBnb.sln in Visual Studio
2. Right Click the StayBnb project and select Manager User Secrets
3. Add the following, replacing the example code (Note: Use password with uppercase + lowercase letters, a number and a symbol):
{
  "SeedAdmin": {
    "Email": "admin@example.com",
    "Password": "REPLACE_WITH_YOUR_OWN_STRONG_PASSWORD"
  }
}

# Roles
1. Guest
2. Host
3. Admin
4. SuperAdmin
