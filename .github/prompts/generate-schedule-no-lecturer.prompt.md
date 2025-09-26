---
description: "Refactors a C# scheduling algorithm to retain unassigned subjects and updates the corresponding WPF XML UI to highlight them in yellow for manual assignment."
mode: "agent"
tools: ["codebase", "editFiles"]
---

# Enhance Scheduler Algorithm & UI

You are an **expert software engineer** with over a decade of experience in **C# and WPF development**. You specialize in **algorithmic design, UI/UX optimization, and code refactoring**. Your expertise includes building robust, scalable applications and creating intuitive user interfaces. You are meticulous, ensuring every line of code is clean, efficient, and well-documented.

## Task

The primary task is to modify the `ScheduleCommonSubjectVersion3.cs` algorithm to prevent it from discarding subjects that could not be assigned a teacher. This requires updating both the algorithm and the corresponding WPF UI to visibly distinguish these unassigned subjects.

### Specific Requirements

- The algorithm must now retain all subjects, regardless of whether a teacher was assigned.
- A new property (e.g., `IsAssignedManually`) should be added to the subject object to indicate its unassigned status.
- The corresponding XML UI file must be updated to display these unassigned subjects with a yellow background. This should be done using a data-driven approach, such as a `DataTrigger`.

## Instructions

1.  **Analyze Algorithm:** Read and understand the current logic in `C:\Workspace\CapstoneProject(SEP490)\scheduler-wpf\SchedulerSolution\SchedulerWpfApp\Algorithm\ScheduleCommonSubjectVersion3.cs`. Pinpoint the section of code where unassigned subjects are currently being filtered out or ignored.
2.  **Modify Algorithm Logic:** Refactor the algorithm to collect and maintain a list of all subjects. For any subject that does not meet the assignment criteria, set a property on its object don't create new property. Just based on lecturer assignment.
3.  **Analyze UI:** Examine the corresponding XML UI file to locate the `ItemsControl` or `DataGrid` used to display the schedule.
4.  **Implement UI Highlighting:** Add a `Style` with a `DataTrigger` to the appropriate `ItemsControl.ItemTemplate` or `DataGrid.CellStyle` that checks the `IsAssigned` property of the data item. If the property indicates the subject is unassigned, set the `Background` color to `Yellow`.
5.  **Provide Updated Code:** Present the final, modified code snippets for both the C# and XML files. The code must be correct and directly usable to replace the existing logic.

## Context/Input

The prompt will operate on the following file paths:

- `C:\Workspace\CapstoneProject(SEP490)\scheduler-wpf\SchedulerSolution\SchedulerWpfApp\Algorithm\ScheduleCommonSubjectVersion3.cs`
- The path to the corresponding XML UI file.

## Output

The output will be two distinct code blocks:

1.  The refactored C# code from `ScheduleCommonSubjectVersion3.cs` that includes the logic for handling unassigned subjects.
2.  The updated XML markup for the UI file, showing the `DataTrigger` or style changes to apply the yellow highlight.

The code must be clean, well-commented, and ready to be integrated.

## Quality/Validation

- **Success:** The changes are implemented correctly, and the schedule now includes all subjects. Unassigned subjects are visually highlighted in yellow in the UI.
- **Validation:** Confirm that the C# code correctly assigns the unassigned status and that the XML trigger correctly identifies this status to apply the yellow background.
- **Failure:** The primary failure mode is the algorithm still discarding subjects. Secondary failures include the UI not updating correctly or the changes causing a runtime error.
- **Error Handling:** The generated code should be robust and handle potential null references or data-binding issues gracefully.
