using GradesManager.DTOs;
using GradesManager.Models;

namespace GradesManager.Extensions
{
    public static class DTOExtensions
    {
        public static CourseDto ToDto(this Course course)
        {
            return new CourseDto(
                CourseId: course.CourseId,
                Code: course.Code,
                Name: course.Name,
                CanvasCourseId: course.CanvasCourseId,
                CanvasWorkflowState: course.CanvasWorkflowState,
                CanvasIsActive: course.CanvasIsActive,
                LastCanvasSyncAt: course.LastCanvasSyncAt
            );
        }

        public static StudentDto ToDto(this Student student)
        {
            return new StudentDto(
                StudentId: student.StudentId,
                Name: student.Name,
                IdealMark: student.IdealMark,
                CanvasUserId: student.CanvasUserId
            );
        }

        public static AssignmentDto ToDto(this Assignment assignment)
        {
            return new AssignmentDto(
                AssignmentId: assignment.AssignmentId,
                CourseId: assignment.CourseId,
                GroupId: assignment.GroupId,
                Name: assignment.Name,
                MaxMark: assignment.MaxMark,
                DueAt: assignment.DueAt,
                UpdatedAt: assignment.UpdatedAt,
                CanvasWorkflowState: assignment.CanvasWorkflowState,
                CanvasSubmissionState: assignment.CanvasSubmissionState,
                CanvasIsActive: assignment.CanvasIsActive,
                CanvasAssignmentId: assignment.CanvasAssignmentId
            );
        }

        public static StudentAssignmentDto ToDto(this StudentAssignment studentAssignment)
        {
            return new StudentAssignmentDto(
                StudentId: studentAssignment.StudentId,
                AssignmentId: studentAssignment.AssignmentId,
                TempMark: studentAssignment.TempMark,
                FinalMark: studentAssignment.FinalMark
            );
        }

        public static AssignmentGroupDto ToDto(this AssignmentGroup assignmentGroup)
        {
            return new AssignmentGroupDto(
                GroupId: assignmentGroup.GroupId,
                CourseId: assignmentGroup.CourseId,
                Name: assignmentGroup.Name,
                Weight: assignmentGroup.Weight,
                CanvasAssignmentGroupId: assignmentGroup.CanvasAssignmentGroupId
            );
        }
    }
}
