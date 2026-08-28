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
                CanvasCourseId: course.CanvasCourseID,
                CanvasIsActive: course.CanvasIsActive,
                LastCanvasSyncAt: course.LastCanvasSyncAt
            );
        }

        public static StudentDto ToDto(this Student student)
        {
            return new StudentDto(
                StudentID: student.StudentID,
                Name: student.Name,
                IdealMark: student.IdealMark
            );
        }

        public static AssignmentDto ToDto(this Assignment assignment)
        {
            return new AssignmentDto(
                AssignmentID: assignment.AssignmentID,
                CourseID: assignment.CourseID,
                Name: assignment.Name,
                MaxMark: assignment.MaxMark,
                Weight: assignment.Weight,
                TempMark: assignment.TempMark,
                FinalMark: assignment.FinalMark,
                Completed: assignment.Completed

            );
        }
    }
}
