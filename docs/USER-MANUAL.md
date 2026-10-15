# User manual

## Administrator

Sign in with corporate SSO using an assigned Administrator role and verified employee record. The dashboard shows employee/program/video/assignment counts and completed/overdue assignments.

1. Create departments, then employee records. Enter the correct worker category, unique email and optional SSO user identifier. Do not mark a contractor or temporary worker as Employee.
2. Open Eligibility Rules to create, edit, deactivate or delete additional exclusions by employee ID, prefix, email domain, worker type or department ID. Rules are case-insensitive where appropriate. Mandatory restrictions cannot be disabled. A rule can exclude administrators too; check its intended scope before saving.
3. Create a training program and any number of videos. Choose unique sequence numbers in viewing order, the private repository filename and correct duration. Use preview to validate the media. A program with zero videos does not unlock an exam.
4. Create the program's exam, questions, options and passing score, then assign the program to employees with an optional due date. Quiz questions and passing criteria freeze after the first attempt to preserve grading history; deactivate or create a revised program when needed.
5. Use Progress or Reports to see video completion, exam state, best score, overall status and due date. Client policy: employees may submit each exam up to three times, including after passing. Taking all three attempts is optional. The best graded score counts; pending manual reviews count toward the attempt limit but are excluded from the best score until graded. Completion requires all videos plus a passed exam. An optional active retake does not remove an already earned completion. Overdue means the due timestamp has elapsed without completion. The highest graded score is shown, and a pass remains valid if a later attempt receives a lower score.
6. Filter Reports by employee, department, program and status; export the current filters to CSV. Audit Logs can be filtered by employee ID and action and paged in batches of 100.

## Employee

Sign in with corporate SSO. Ineligible or unrecognized accounts cannot enter the portal; contact the training administrator to correct an inaccurate employee record.

Open My Training, choose an assignment, and watch the first available video at normal speed. Later videos remain locked until earlier videos finish. Saved progress updates regularly and on pause. Reloading resumes the last accepted position. If the network fails or the server rejects playback, reload to resume.

After all videos finish, open Exams and start the assessment. Training becomes unavailable during the attempt, including other open training tabs. Answer every question and submit. Results show your server-calculated score and pass/fail status. My Progress displays consolidated assignment completion.

The private media route prevents anonymous access, but a browser is not a DRM environment. Do not distribute training recordings or copy media outside the portal.

Deleting a training program: choose Delete from Training Programs and confirm Archive training program. This removes the program from the program list and new assignment choices. Reports, assignments, viewing progress, exam questions, attempts, results, and uploaded files are retained. Employees already assigned can continue training. Previously deleted records are not restored by this change.


## Questions: manual entry, CSV upload and keyword marking

In Exams > Manage, use **Add question** for individual entry or **Import CSV** for a batch. Both options remain available. Exams can mix multiple-choice and typed questions. Each question has equal weight, and typed answers allow up to 5,000 characters. Questions cannot change after an exam has any attempts.

For typed questions, enter **Required keywords or phrases**, separated by semicolons (for example `isolate;power supply`). An answer earns 1 point only when all configured words or phrases occur; otherwise it earns 0. Matching ignores case, punctuation and extra spaces, and matches complete words, not substrings (`safe` does not match `unsafe`). Phrases must appear in order. This is literal matching: it does not detect negation, evaluate meaning, or recognize synonyms. Choose keywords accordingly. Keywords are never included in the employee exam payload.

An exam containing only MCQs and keyword-configured typed questions receives its final score immediately on submission. Leaving keywords blank in the manual form retains manual review, including for existing questions. Exams with any such question show **Awaiting review** until an administrator grades those answers. Submission ends the active attempt and releases the training playback lock.

Administrators open **Exams > Review typed answers**, select a submission, and mark the answers that require review. Keyword marks are displayed separately and preserved. **Publish result** combines manual, keyword and multiple-choice marks and applies the passing percentage. Published results cannot be marked again.

### CSV question format

Download the CSV template from **Import CSV**, replace its sample rows, and save as UTF-8 comma-delimited CSV. Keep these headers in this order:

`QuestionText,AnswerType,OptionA,OptionB,OptionC,OptionD,CorrectOption,AnswerKeywords,SequenceOrder`

- `AnswerType`: `MCQ` or `Typed`.
- MCQs require all four options and `CorrectOption` A, B, C or D.
- Typed rows require `AnswerKeywords` for automatic grading. Leave option columns blank.
- `SequenceOrder` must be a positive number, unique in the file and not already used in the exam.
- Put text containing commas or line breaks inside double quotes. Double any quotes inside quoted text.

Uploads append up to 500 questions, maximum 1 MB. An invalid row rejects the entire upload and reports the issue; no partial import is saved. Re-uploading the same orders is rejected. Uploaded files are not stored on the server. Manual editing and deletion remain available before attempts exist.

## Assign training to a group

Open Training Assignments > Assign Training and choose a ready program.

- **Filter and select:** choose a department and/or search by name or employee ID. Select all matching employees, or use individual checkboxes. Selections remain selected when filters change; Clear selection clears the whole selection.
- **Upload CSV:** download the template and fill the EmployeeId column with existing employee IDs, one per row. Save as comma-delimited CSV. Preserve leading zeros. Additional columns are ignored. Maeimum 1,000 rows and 128 KB; the uploaded file is not saved on the server.

Set Assigned on and an optional Due by in IST for the whole group. Click **Preview assignment** to see ready employees and skipped rows, including duplicates, already assigned employees, unknown IDs and ineligible employees. No assignments are created during preview.

Click **Confirm assignment** within 30 minutes. Only the employees approved in that preview are considered. The server rechecks program readiness, eligibility and existing assignments before saving, so changing eligibility or assigning the program from another session cannot create duplicate assignments. If program setup changes, preview again. Employees who become ineligible or already assigned are skipped. Repeat confirmation never creates duplicate assignments.
