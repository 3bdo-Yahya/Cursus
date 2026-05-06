# Entities (Models/Entities)

## Overview

This folder contains the main domain entities used by the application.
Each class represents a business object that is persisted in SQL Server through Entity Framework Core.

These entities model:

- universities and departments
- courses and prerequisites
- students and their academic history
- graduation rules and grade rules

Entity classes focus on domain meaning, validation attributes, and navigation properties.
Database-specific mapping details such as indexes, composite keys, and delete behavior are defined in `Data/Configurations`.

See also:

- [../Enums/README.md](../Enums/README.md)
- [../../Data/Configurations/README.md](../../Data/Configurations/README.md)

---

## Entity Groups

### 1. Academic Structure

#### `University`

Represents a university or institution.

Main responsibilities:

- stores the university name
- acts as the parent of departments
- acts as the parent of grade scales

Relations:

- one `University` has many `Departments`
- one `University` has many `GradeScales`

#### `Department`

Represents an academic department, major, or program inside a university.

Main responsibilities:

- stores department identity
- stores total graduation credits
- stores minimum GPA required for graduation

Relations:

- belongs to one `University`
- has many `Courses`
- has many `AppUser` students
- has many `GraduationRequirements`
- has many `CreditHourRules`

#### `Course`

Represents a course in the catalog.

Main responsibilities:

- stores course code, name, and credit hours
- stores course category through `CourseType`
- stores semester offering through `SemesterAvailability`
- stores passing grade threshold
- stores whether the course is active

Relations:

- belongs to one `Department`
- has many `StudentCourses`
- has many prerequisite links through `CoursePrerequisite`
- has many graduation requirement links through `GraduationRequirementCourse`

---

### 2. Student And Identity

#### `AppUser`

Represents the application user and extends ASP.NET Identity's `IdentityUser`.

Main responsibilities:

- stores the authenticated student identity
- stores department assignment
- stores current semester and academic standing
- stores current academic year

Relations:

- optionally belongs to one `Department`
- has many `StudentCourses`
- has many `StandingHistories`

#### `StudentCourse`

Represents a student's record for a specific course in a specific semester and academic year.

Main responsibilities:

- links a student to a course
- stores course status through `StudentCourseStatus`
- stores grade if available
- stores semester and academic year

Relations:

- belongs to one `AppUser`
- belongs to one `Course`

#### `StandingHistory`

Represents a student's academic standing snapshot for a semester.

Main responsibilities:

- stores semester GPA
- stores cumulative GPA
- stores academic standing through `AcademicStanding`
- stores semester and academic year

Relations:

- belongs to one `AppUser`

---

### 3. Academic Rules

#### `GradeScale`

Represents grade-to-point conversion at the university level.

Main responsibilities:

- stores a letter grade such as `A` or `B+`
- stores the numeric point value

Relations:

- belongs to one `University`

#### `CreditHourRule`

Represents allowed credit-hour load for a department based on standing.

Main responsibilities:

- stores the academic standing
- stores minimum and maximum allowed credits

Relations:

- belongs to one `Department`

#### `GraduationRequirement`

Represents a graduation rule for a department by course category.

Main responsibilities:

- stores the category through `CourseType`
- stores required credits for that category

Relations:

- belongs to one `Department`
- has many `GraduationRequirementCourse` links

---

### 4. Junction Entities

#### `CoursePrerequisite`

Represents a self-referencing relationship between a course and another course that must be completed first.

Main responsibilities:

- stores the dependent course
- stores the prerequisite course

Relations:

- belongs to one `Course`
- belongs to one prerequisite `Course`

#### `GraduationRequirementCourse`

Represents the mapping between a graduation requirement and the courses that can satisfy it.

Main responsibilities:

- links requirement rows to eligible courses

Relations:

- belongs to one `GraduationRequirement`
- belongs to one `Course`

---

## Relationship Summary

| Entity | Key Relations |
| --- | --- |
| `University` | `Departments`, `GradeScales` |
| `Department` | `University`, `Students`, `Courses`, `GraduationRequirements`, `CreditHourRules` |
| `Course` | `Department`, `Prerequisites`, `IsPrerequisiteFor`, `StudentCourses`, `GraduationRequirementCourses` |
| `AppUser` | `Department`, `StudentCourses`, `StandingHistories` |
| `StudentCourse` | `Student`, `Course` |
| `StandingHistory` | `Student` |
| `CreditHourRule` | `Department` |
| `GradeScale` | `University` |
| `GraduationRequirement` | `Department`, `GraduationRequirementCourses` |
| `CoursePrerequisite` | `Course`, `Prerequisite` |
| `GraduationRequirementCourse` | `GraduationRequirement`, `Course` |

---

## Important Notes

- Entity classes contain validation attributes such as `[Required]`, `[StringLength]`, and `[Range]`.
- Enums used in these entities are stored as integers in the database by default.
- Navigation properties describe relationships, but the final database behavior is controlled in the Fluent API configuration files.
- Junction entities are modeled explicitly because they are important parts of the academic domain, not just hidden technical join tables.

---

## Best Practices

- Keep entity classes focused on business meaning.
- Put SQL-specific rules in configuration classes, not directly in entities unless an attribute is simple and intentional.
- When adding a new entity, also add:
  - a matching configuration class if relational rules are needed
  - a `DbSet<>` in `ApplicationDbContext`
  - a migration after the model is updated

---

## Next Step

After understanding the entity classes, read the EF Core mapping files in:

- [../../Data/Configurations/README.md](../../Data/Configurations/README.md)

That is where indexes, composite keys, delete behavior, and relational constraints are defined.
