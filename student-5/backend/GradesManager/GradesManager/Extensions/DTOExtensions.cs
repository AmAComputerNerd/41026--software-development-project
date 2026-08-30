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
                CanvasIsActive: course.CanvasIsActive,
                LastCanvasSyncAt: course.LastCanvasSyncAt
            );
        }

        public static StudentDto ToDto(this Student student)
        {
            return new StudentDto(
                StudentId: student.StudentId,
                Name: student.Name,
                IdealMark: student.IdealMark
            );
        }

        public static AssignmentDto ToDto(this Assignment assignment)
        {
            return new AssignmentDto(
                AssignmentId: assignment.AssignmentId,
                CourseId: assignment.CourseId,
                Name: assignment.Name,
                MaxMark: assignment.MaxMark,
                Weight: assignment.Weight,
                Completed: assignment.Completed

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
    }
}
