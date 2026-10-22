# Staybnb

An Airbnb-style property rental platform built with ASP.NET Core MVC (.NET 8), EF Core and SQL Server LocalDB.
Programming 742 assignment.

## Roles

| Role | How you get it |
|------|----------------|
| Guest | Automatic when you register |
| Host | When an Admin approves your host application (Guest role is removed) |
| Admin | Promoted by the SuperAdmin |
| SuperAdmin | Seeded on startup from `appsettings.json` |

## Running the project

1. Install the EF tool once: `dotnet tool install --global dotnet-ef --version 8.*`
2. From the `Staybnb` project folder, create the database tables:
   `dotnet ef migrations add InitialIdentity`
3. Run the app: `dotnet run` (the database, roles and SuperAdmin are created on startup)

Default SuperAdmin (change in `appsettings.json`): `superadmin@staybnb.com` / `SuperAdmin@123`

## Progress

- [x] Phase 1 - Solution setup, Identity, roles, SuperAdmin seeder
- [x] Phase 2 - Models, DbContext, migrations
- [x] Phase 3 - Host application flow
- [x] Phase 4 - Host dashboard, admin approval, user promotion, activity log
- [x] Phase 5 - Guest browsing, booking, check-in
- [x] Phase 6 - Admin dashboard
- [x] Phase 7 - Messaging, notifications, activity logging
- [x] Phase 8 - Seed data, testing, final polish

## Demo accounts (created automatically on first run)

| Role | Email | Password |
|------|-------|----------|
| SuperAdmin | superadmin@staybnb.com | SuperAdmin@123 |
| Admin | admin@staybnb.com | Admin@12345 |
| Host | host@staybnb.com | Host@12345 |
| Guest | guest@staybnb.com | Guest@12345 |

The sample host owns four active properties. The sample guest has one pending and one approved booking.

## Main features

- Roles: Guest, Host, Admin, SuperAdmin with strict `[Authorize(Roles = ...)]`
- Host application with photo upload, approved by an Admin (Guest role swapped for Host)
- Host dashboard: properties, activate/deactivate, booking requests, check-in rules
- Guest gallery with city/type filters, booking engine (total = price x nights + cleaning fee + service fee)
- Check-in workflow with ID/passport upload and host verification (Pending, Approved, CheckedIn)
- Admin dashboard, SuperAdmin user promotion, activity log
- Database-backed messaging inbox and notification bell (read/unread)
