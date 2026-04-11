# SeedData Package

This folder is the final data package for three universities. It contains only the published seed JSON files that the team needs to load and use.

## Folder Structure

```text
SeedData/
|-- README.md
|-- american-university-in-cairo/
|   |-- curriculum-courses.json
|   |-- graduation-requirements.json
|   |-- normalization-notes.json
|   |-- seed_graduation_reqs.json
|   `-- seed_prereqs.json
|-- sinia-university/
|   |-- curriculum-courses.json
|   |-- graduation-requirements.json
|   |-- normalization-notes.json
|   |-- seed_graduation_reqs.json
|   `-- seed_prereqs.json
`-- south-valley-university/
    |-- curriculum-courses.json
    |-- graduation-requirements.json
    |-- normalization-notes.json
    |-- seed_graduation_reqs.json
    `-- seed_prereqs.json
```

## Universities Included

| Folder slug | University name | Course rows |
| --- | --- | ---: |
| `south-valley-university` | South Valley University | 134 |
| `american-university-in-cairo` | The American University in Cairo | 414 |
| `sinia-university` | Sinai University | 102 |


## What Each File Does

### `curriculum-courses.json`

This is the main course catalog for the university.

Use it for:

- course code and title
- description
- credit hours and contact hours
- semester availability
- prerequisite groups
- course category by major through `programRules`

This is the primary file the application should load first.

### `seed_prereqs.json`

This is the flat prerequisite mapping table.

Use it when the database or seed pipeline needs explicit prerequisite rows like:

```json
{
  "courseCode": "CS201",
  "prerequisiteCourseCode": "CS101"
}
```

### `seed_graduation_reqs.json`

This is the flat graduation-requirements seed table.

For South Valley and AUC, rows are category-based:

```json
{
  "categoryType": "Core",
  "requiredCredits": 69,
  "eligibleCourseCodes": ["CS101", "CS102"]
}
```

For Sinai, rows are major-specific:

```json
{
  "major": "IT",
  "categoryType": "Core",
  "requiredCredits": 102,
  "eligibleCourseCodes": ["CSW110", "CSW121"]
}
```

### `graduation-requirements.json`

This is the readable summary of degree rules.

Use it for:

- displaying graduation rules in admin or docs
- checking required credit totals
- showing category breakdowns

Structure difference:

- South Valley and AUC use top-level `categories`
- Sinai uses `majors -> categories`

### `normalization-notes.json`

This is a reference file for audit and debugging.

Use it to understand:

- code corrections
- source conflicts
- inferred values
- normalization decisions

This file is useful for team understanding, but it is usually not primary runtime data.

## Recommended Usage Flow

For each university:

1. Select the folder by slug.
2. Load `curriculum-courses.json` as the canonical source.
3. Load `seed_prereqs.json` if your database needs flattened prerequisite rows.
4. Load `seed_graduation_reqs.json` if your database needs flattened graduation-category rows.
5. Use `graduation-requirements.json` for readable totals and category summaries.
6. Use `normalization-notes.json` only for audit, QA, and debugging.

## How To Work With All 3 Universities

Use the same loading pattern for all three folders. The schema is aligned across them, but there are two important differences:

### South Valley University

- One university folder supports multiple majors.
- Major-specific placement is stored in `programRules`.
- Graduation rows are category-based, not major-specific.

Majors found in the data:

- `CS`
- `IT`
- `IS`
- `AI`

### The American University in Cairo

- Stored under `american-university-in-cairo`.
- The dataset is mainly a CS curriculum package.
- Graduation rows are category-based, not major-specific.

Major found in the data:

- `CS`

### Sinai University

- Stored under `sinia-university`.
- Multiple majors exist in the same folder.
- Graduation requirements are major-specific.

Majors found in the data:

- `IT`
- `CSSE`
- `IDSS`

### Author
Mr Tawfik
