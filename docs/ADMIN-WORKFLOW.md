# Administrator workflow

1. Create a department, then add employees with verified worker categories and corporate identity details. If employee creation has no departments, its setup link returns to employee creation after the department is saved.
2. Create a program with its title and description. Empty programs remain incomplete and cannot be assigned.
3. Add videos. The program is preselected when entering from its setup page, and the next sequence number is suggested. Use Move up / Move down before employee viewing starts.
4. Create an exam draft and add questions. New questions use the next available order; Save and add another keeps the author in question entry.
5. Open Exam availability after adding videos and questions. Choose all videos or a named video. The rule makes the exam available; employees still choose when to start it.
6. Activate the exam through Edit exam. Activation requires videos and questions. Program setup displays remaining prerequisites or Ready to assign.
7. Assign one or more eligible employees. Review the selected program and its exam availability, then enter an optional due date. Assignment forms display India Standard Time (IST); dates are stored in UTC. Assigned on defaults to the current time, and the due date is optional. Use Filter and select (department, name or employee ID) or Upload CSV. Preview the group, then confirm. Duplicate IDs, existing assignments, unknown IDs and ineligible employees are skipped with reasons.
8. Monitor the same status in assignments, progress, and reports. Reports support filtering, exporting, and links to employee/program details.

## Maintenance

- Started assignments can have their dates updated but cannot be moved to another employee or program.
- Removing an assignment retains progress and exam history. Reassigning the same program reuses that history.
- Program deletion archives the program, removes it from new assignment choices, and retains reports and all linked learning records. Existing employees can continue assigned training.
- Employees with learning records cannot be deleted. Review assignments and eligibility instead.
- Eligibility exclusions have department/category selectors and a Preview impact action. Preview does not save. Active rules apply to administrators as well as employees.
- Audit history supports employee, action, and UTC date filters.
- Completion continues to require all videos and a passed exam. Video-only completion is not enabled.

## Validation

HTTP regression tests cover missing prerequisites, draft and ineligible assignment rejection, program context, exam draft/activation, availability, bulk assignment, ordering, eligibility preview, and deletion feedback. Tests use isolated EF in-memory databases; they do not modify live training data or establish SQL Server transaction behavior.
