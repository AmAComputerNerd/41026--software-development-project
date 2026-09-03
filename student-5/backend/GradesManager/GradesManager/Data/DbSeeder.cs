using GradesManager.Models;

namespace GradesManager.Data
{
    public static class DbSeeder
    {
        public static void SeedData(AppDbContext db)
        {
            var courses = SeedCourses(db);
            var students = SeedStudents(db);
            SeedSC(db, courses, students);
            var groups = SeedAG(db, courses);
            var assignments = SeedAssignments(db, courses, groups);
            SeedSA(db, students, assignments);
        }

        private static List<Course> SeedCourses(AppDbContext db)
        {
            var courses = db.Courses;
            if (!courses.Any())
            {
                courses.AddRange(
                    new Course //C1
                    {
                        Code = "HM-39",
                        Name = "Intro to Voice Synthesiser Development"
                    },

                    new Course //C2
                    {
                        Code = "CV-02",
                        Name = "Advanced Voice Synthesiser Development"
                    },

                    new Course //C3
                    {
                        Code = "MH-05",
                        Name = "Introduction to New World Ecology"
                    },

                    new Course //C4
                    {
                        Code = "GO-KU",
                        Name = "Computer Game Programming"
                    },

                    new Course //C5
                    {
                        Code = "PK-MN",
                        Name = "Game Design Methodologies"
                    },

                    new Course //C6
                    {
                        Code = "RK-GK",
                        Name = "Beginner Sketching"
                    },

                    new Course //C7
                    {
                        Code = "SU-GK",
                        Name = "Discrete Mathematics"
                    },

                    new Course //C8
                    {
                        Code = "FA-18",
                        Name = "Aerospace Engineering Studio"
                    },

                    new Course //C9
                    {
                        Code = "WA-GK",
                        Name = "Understanding Traditional Compositions"
                    },

                    new Course //C10
                    {
                        Code = "YG-OH",
                        Name = "Card Game Design Studio"
                    }
                );

                db.SaveChanges();
            }

            return courses.ToList();
        }

        private static List<Student> SeedStudents(AppDbContext db)
        {
            var students = db.Students;
            if (!students.Any())
            {
                students.AddRange(
                    new Student //S1
                    {
                        Name = "Johnathon Thompson",
                        IdealMark = 85.0
                    },

                    new Student //S2
                    {
                        Name = "Issac Binding",
                        IdealMark = 90.0
                    },

                    new Student //S3
                    {
                        Name = "John MonsterHunter",
                        IdealMark = 75.0
                    },

                    new Student //S4
                    {
                        Name = "Dave Seaweed",
                        IdealMark = 80.0
                    },

                    new Student //S5
                    {
                        Name = "Zaquaphy Seaweed",
                        IdealMark = 77.0
                    },

                    new Student //S6
                    {
                        Name = "Charlie Brown",
                        IdealMark = 73.0
                    },

                    new Student //S7
                    {
                        Name = "John Citizen",
                        IdealMark = 65.0
                    },

                    new Student //S8
                    {
                        Name = "Kagamine Rin",
                        IdealMark = 95.0
                    },

                    new Student //S9
                    {
                        Name = "Seto Kaiba",
                        IdealMark = 75.0
                    },

                    new Student //S10
                    {
                        Name = "Hatsune Miku",
                        IdealMark = 39.0
                    }
                );
                db.SaveChanges();
            }
            return students.ToList();
        }

        private static List<StudentCourse> SeedSC(AppDbContext db, List<Course> courses, List<Student> students)
        {
            var scs = db.StudentCourses;
            if (!scs.Any())
            {
                var count = Math.Min(10, Math.Min(courses.Count, students.Count));
                for (int i = 0; i < count; i++)
                {
                    scs.Add(new StudentCourse
                    {
                        StudentId = students[i].StudentId,
                        CourseId = courses[i].CourseId
                    });
                }
                db.SaveChanges();
            }
            return scs.ToList();
        }

        private static List<Assignment> SeedAssignments(AppDbContext db, List<Course> courses, List<AssignmentGroup> groups)
        {
            var assignments = db.Assignments;
            if (!assignments.Any())
            {
                assignments.AddRange(
                    new Assignment //A1
                    {
                        CourseId = courses[0].CourseId,
                        Name = "Voice sample aquisition",
                        MaxMark = 1,
                        GroupId = groups[0].GroupId

                    },

                    new Assignment //A2
                    {
                        CourseId = courses[0].CourseId,
                        Name = "Basic synthesiser program",
                        MaxMark = 25,
                        GroupId = groups[0].GroupId
                    },

                    new Assignment //A3
                    {
                        CourseId = courses[1].CourseId,
                        Name = "Advanced Synthesiser program",
                        MaxMark = 30,
                        GroupId = groups[0].GroupId
                    },

                    new Assignment //A4
                    {
                        CourseId = courses[1].CourseId,
                        Name = "Twin voice sampling",
                        MaxMark = 10,
                        GroupId = groups[1].GroupId
                    },

                    new Assignment //A5
                    {
                        CourseId = courses[0].CourseId,
                        Name = "Final Test",
                        MaxMark = 50,
                        GroupId = groups[1].GroupId
                    },

                    new Assignment //A6
                    {
                        CourseId = courses[1].CourseId,
                        Name = "Lab Quiz 1",
                        MaxMark = 5,
                        GroupId = groups[2].GroupId
                    },

                    new Assignment //A7
                    {
                        CourseId = courses[1].CourseId,
                        Name = "Lab Quiz 2",
                        MaxMark = 5,
                        GroupId = groups[2].GroupId
                    },

                    new Assignment //A8
                    {
                        CourseId = courses[1].CourseId,
                        Name = "Lab Quiz 3",
                        MaxMark = 5,
                        GroupId = groups[2].GroupId
                    },

                    new Assignment //A9
                    {
                        CourseId = courses[2].CourseId,
                        Name = "Mid term Ecology Test",
                        MaxMark = 30,
                        GroupId = groups[3].GroupId
                    },

                    new Assignment //A10
                    {
                        CourseId = courses[2].CourseId,
                        Name = "Final Practical Exam",
                        MaxMark = 50,
                        GroupId = groups[3].GroupId
                    }
                );
                db.SaveChanges();
            }
            return assignments.ToList();
        }

        private static List<StudentAssignment> SeedSA(AppDbContext db, List<Student> students, List<Assignment> assignments)
        {
            var sas = db.StudentAssignments;
            if (!sas.Any())
            {
                var count = Math.Min(10, Math.Min(assignments.Count, students.Count));
                for (int i = 0; i < count; i++)
                {
                    sas.Add(new StudentAssignment
                    {
                        StudentId = students[i].StudentId,
                        AssignmentId = assignments[i].AssignmentId
                    });
                }
                db.SaveChanges();
            }
            return sas.ToList();
        }

        private static List<AssignmentGroup> SeedAG(AppDbContext db, List<Course> courses)
        {
            var ags = db.AssignmentGroups;
            if (!ags.Any())
            {
                var count = Math.Min(10, courses.Count);
                for (int i = 0; i < count; i++)
                {
                    ags.Add(new AssignmentGroup
                    {
                        CourseId = courses[i].CourseId,
                        Name = $"Assignment Group {i + 1}",
                        Weight = 100
                    });
                }
                db.SaveChanges();
            }
            return ags.ToList();
        }
    }
}
