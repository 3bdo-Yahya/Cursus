# Enums (Cursus.Domain/Enums)

## Overview

This folder contains all enumeration types used across the domain model.
Enums represent fixed sets of values defined in the system based on the SRS (Section 15 Data Dictionary).

Using enums improves:

* Code readability
* Type safety
* Consistency between application logic and database values

All enums are stored as **integers in the database** by default using Entity Framework Core.

See also:

* [../Entities/README.md](../Entities/README.md)
* [../../Data/Configurations/README.md](../../Data/Configurations/README.md)

---

## List of Enums

### 1. CourseType

Defines the category of a course.

| Value | Name          | Description                 |
| ----- | ------------- | --------------------------- |
| 0     | Core          | Mandatory department course |
| 1     | DeptElective  | Department elective course  |
| 2     | FreeElective  | Free elective course        |
| 3     | UniversityReq | University requirement      |

---

### 2. SemesterAvailability

Specifies when a course is offered.

| Value | Name       | Description                |
| ----- | ---------- | -------------------------- |
| 0     | Fall       | Fall semester only         |
| 1     | Spring     | Spring semester only       |
| 2     | FallSpring | Fall and Spring            |
| 3     | All        | Available in all semesters |

---

### 3. AcademicStanding

Represents the academic status of a student.

| Value | Name      | Description      |
| ----- | --------- | ---------------- |
| 0     | Good      | Good standing    |
| 1     | Warning   | Academic warning |
| 2     | Probation | On probation     |
| 3     | Dismissed | Dismissed        |

---

### 4. StudentCourseStatus

Indicates the status of a student's course.

| Value | Name       | Description        |
| ----- | ---------- | ------------------ |
| 0     | Completed  | Course completed   |
| 1     | Failed     | Course failed      |
| 2     | InProgress | Currently enrolled |

---

### 5. SemesterType

Represents academic semesters.

| Value | Name   | Description     |
| ----- | ------ | --------------- |
| 0     | Fall   | Fall semester   |
| 1     | Spring | Spring semester |
| 2     | Summer | Summer semester |

---

## Notes

* Enum values are explicitly assigned to match the SRS specification.
* These values must **not be changed** after deployment to avoid data inconsistency.
* EF Core stores enums as integers unless configured otherwise.
* Enums are used in multiple entities such as:

  * `Course`
  * `StudentCourse`
  * `StandingHistory`
  * `CreditHourRule`
  * `AppUser`

---

## Best Practices

* Always use enums instead of magic numbers.
* Do not hardcode integer values in your business logic.
* When displaying enum values in the UI, convert them to readable text if needed.

---

## Next Step

After defining enums, proceed to implement the core entity classes:

* University
* Department
* Course

These will use the enums defined in this folder.

Then read:

* [../Entities/README.md](../Entities/README.md)
* [../../Data/Configurations/README.md](../../Data/Configurations/README.md)
