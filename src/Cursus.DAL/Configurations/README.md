# Configurations (Data/Configurations)

## Overview

This folder contains Entity Framework Core Fluent API configuration classes.
Each file implements `IEntityTypeConfiguration<T>` for one entity and defines how that entity is mapped to SQL Server.

These configuration classes are responsible for rules such as:

- property length and precision
- indexes
- unique constraints
- composite keys
- foreign keys
- delete behavior
- check constraints

The configurations are loaded automatically by `ApplicationDbContext` using:

```csharp
builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
```

See also:

- [../../Models/Enums/README.md](../../Models/Enums/README.md)
- [../../Models/Entities/README.md](../../Models/Entities/README.md)

---

## Why Use Configurations

Keeping mapping rules here gives the project a cleaner structure:

- entity classes stay focused on domain meaning
- database rules are centralized in one place
- migrations become easier to understand
- schema changes are easier to review as a team

---

## Configuration Files

### `AppUserConfiguration`

Configures the custom Identity user.

Main rules:

- sets `AcademicYear` max length
- maps optional relationship to `Department`
- uses `DeleteBehavior.SetNull` when a department is deleted
- adds indexes for department-based student lookup

### `CourseConfiguration`

Configures the course catalog table.

Main rules:

- makes `Code`, `Name`, and `PassingGradeThreshold` required
- sets string length limits
- maps required relationship to `Department`
- creates an index on `DepartmentId`
- enforces unique course code inside each department

### `CoursePrerequisiteConfiguration`

Configures the course prerequisite junction table.

Main rules:

- defines a composite primary key on `CourseId` and `PrerequisiteId`
- adds a check constraint preventing a course from being its own prerequisite
- configures the two self-referencing foreign keys to `Course`
- uses `DeleteBehavior.Restrict` to protect the prerequisite graph
- adds an index on `PrerequisiteId`

### `CreditHourRuleConfiguration`

Configures department credit-load rules.

Main rules:

- maps relationship to `Department`
- adds lookup indexes by department and standing

### `DepartmentConfiguration`

Configures the department table.

Main rules:

- sets name length and required status
- sets decimal precision for `MinGpaForGraduation`
- maps relationship to `University`
- adds unique department name per university

### `GradeScaleConfiguration`

Configures grade scale rows.

Main rules:

- sets letter-grade length and required status
- sets decimal precision for `PointValue`
- maps relationship to `University`
- enforces unique grade letter inside each university

### `GraduationRequirementConfiguration`

Configures graduation requirement rows.

Main rules:

- maps relationship to `Department`
- adds an index on `DepartmentId`

### `GraduationRequirementCourseConfiguration`

Configures the requirement-to-course junction table.

Main rules:

- defines a composite primary key
- maps requirement and course foreign keys
- uses `DeleteBehavior.Restrict` on the course relationship
- adds an index on `CourseId`

### `StandingHistoryConfiguration`

Configures student standing history.

Main rules:

- sets `AcademicYear` as required with max length
- sets precision for GPA fields
- maps relationship to `AppUser`
- enforces uniqueness for one standing row per student, semester, and academic year
- adds lookup indexes for history queries

### `StudentCourseConfiguration`

Configures student course records.

Main rules:

- requires `StudentId`
- limits `Grade` length
- requires `AcademicYear` with max length
- maps relationships to `AppUser` and `Course`
- uses `DeleteBehavior.Restrict` on the course relationship
- enforces one row per student, course, semester, and academic year
- adds indexes for common queries

---

## Common Patterns In This Folder

### Composite Keys

Used for explicit junction tables:

- `CoursePrerequisite`
- `GraduationRequirementCourse`

These prevent duplicate pairs and make the relationship explicit in the schema.

### Unique Indexes

Used to protect domain rules such as:

- course code uniqueness inside a department
- department name uniqueness inside a university
- grade letter uniqueness inside a university
- one student-course row per semester and academic year
- one standing history row per semester and academic year

### Delete Behaviors

The delete behavior is chosen based on business meaning:

- `Cascade` when child rows should disappear with the parent
- `Restrict` when deleting the parent would destroy important history or graph structure
- `SetNull` when the relation is optional and the child can still exist meaningfully

---

## How This Folder Connects To Migrations

When you change a configuration file, EF Core may detect a schema change.
Typical flow:

1. update the entity and-or configuration
2. create a migration
3. review the generated migration
4. apply it to the database

Example:

```bash
dotnet ef migrations add <MigrationName> --project src/Cursus.csproj
dotnet ef database update --project src/Cursus.csproj
```

---

## Best Practices

- Prefer Fluent API here for indexes, relationships, and composite keys.
- Keep configurations small and one-entity-per-file.
- Use meaningful constraint names when adding custom checks.
- Review generated migrations after every configuration change.
- Be careful when changing delete behavior because it can affect production data.

---

## Next Step

If you are tracing a schema issue, read in this order:

1. the entity in `Cursus.Domain/Entities`
2. the matching configuration in this folder
3. `ApplicationDbContext`
4. the generated migration file in the EF Core migrations location for this solution
