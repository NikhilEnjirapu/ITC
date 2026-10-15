# User manual

## Administrator

Sign in with corporate SSO using an assigned Administrator role and verified employee record. The dashboard shows employee/program/video/assignment counts and completed/overdue assignments.

1. Create departments, then employee records. Enter the correct worker category, unique email and optional SSO user identifier. Do not mark a contractor or temporary worker as Employee.
2. Open Eligibility Rules to create, edit, deactivate or delete additional exclusions by employee ID, prefix, email domain, worker type or department ID. Rules are case-insensitive where appropriate. Mandatory restrictions cannot be disabled. A rule can exclude administrators too; check its intended scope before saving.
3. Create a training program and any number of videos. Choose unique sequence numbers in viewing order, the private repository filename and correct duration. Use preview to validate the media. A program with zero videos does not unlock an exam.
4. Create the program's exam, questions, options and passing score, then assign the program to employees with an optional due date. Quiz questions and passing criteria freeze after the first attempt to preserve grading history; deactivate or create a revised program when needed.
5. Use Progress or Reports to see video completion, exam state, best score, overall status and due date. Employees may submit each exam up to three times. Completion requires all videos plus a passed exam, with no active attempt. Overdue means the due timestamp has elapsed without completion. The highest graded score is shown, and a pass remains valid if a later attempt receives a lower score.
6. Filter Reports by employee, department, program and status; export the current filters to CSV. Audit Logs can be filtered by employee ID and action and paged in batches of 100.

## Employee

Sign in with corporate SSO. Ineligible or unrecognized accounts cannot enter the portal; contact the training administrator to correct an inaccurate employee record.

Open My Training, choose an assignment, and watch the first available video at normal speed. Later videos remain locked until earlier videos finish. Saved progress updates regularly and on pause. Reloading resumes the last accepted position. If the network fails or the server rejects playback, reload to resume.

After all videos finish, open Exams and start the assessment. Training becomes unavailable during the attempt, including other open training tabs. Answer every question and submit. Results show your server-calculated score and pass/fail status. My Progress displays consolidated assignment completion.

The private media route prevents anonymous access, but a browser is not a DRM environment. Do not distribute training recordings or copy media outside the portal.

Deleting a training program: choose Delete from Training Programs and review the permanent-deletion warning. Confirming removes the program, video records, assignments, employee viewing progress, exam questions, attempts, and results together. Employee accounts, other programs, audit history, and uploaded video files are retained. This action cannot be undone. Individual video and exam deletion restrictions still apply when deleting those items separately.


## Typed answers and manual review

In Exams > Manage > Add question, choose **Typed answer (manual review)** instead of Multiple choice. Exams may mix both formats. Each question has equal weight; typed answers allow up to 5,000 characters. Questions remain locked after an attempt eeists.

Employees type their answers and submit as usual. An exam containing typed answers shows **Awaiting review**, without a provisional score or pass/fail outcome. Submission ends the active attempt and releases the training playback lock.

Administrators open **Exams > Review typed answers**, select a submission, and mark every typed answer correct (1 point) or incorrect (0 points). **Publish result** combines those marks with automatic multiple-choice marks and applies the exam's passing percentage. Marks, reviewer identity and review time are saved. Published results cannot be marked again. Reports and employee results reflect the final outcome.


## Assign training to a group

Open Training Assignments > Assign Training and choose a ready program.

- **Filter and select:** choose a department and/or search by name or employee ID. Select all matching employees, or use individual checkboxes. Selections remain selected when filters change; Clear selection clears the whole selection.
- **Upload CSV:** download the template and fill the EmployeeId column with existing employee IDs, one per row. Save as comma-delimited CSV. Preserve leading zeros. Additional columns are ignored. Maeimum 1,000 rows and 128 KB; the uploaded file is not saved on the server.

Set Assigned on and an optional Due by in IST for the whole group. Click **Preview assignment** to see ready employees and skipped rows, including duplicates, already assigned employees, unknown IDs and ineligible employees. No assignments are created during preview.

Click **Confirm assignment** within 30 minutes. Only the employees approved in that preview are considered. The server rechecks program readiness, eligibility and existing assignments before saving, so changing eligibility or assigning the program from another session cannot create duplicate assignments. If program setup changes, preview again. Employees who become ineligible or already assigned are skipped. Repeat confirmation never creates duplicate assignments.
