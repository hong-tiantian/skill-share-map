using SkillShareMap.Models;

namespace SkillShareMap.Data;

public static class SeedData
{
    public static void Initialize(ApplicationDbContext context)
    {
        var userSeeds = new List<User>
        {
            new User
            {
                Username = "alice_student",
                Email = "alice@university.edu",
                PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("password123")),
                Role = UserRole.Student,
                FirstName = "Alice",
                LastName = "Johnson",
                SchoolName = "University of Technology Sydney",
                Bio = "Computer Science student, love coding!",
                IsVerified = true,
                HomeBaseLatitude = -33.8836,
                HomeBaseLongitude = 151.2006,
                HomeBaseAddress = "15 Broadway, Ultimo NSW 2007",
                ReputationLevel = 0
            },
            new User
            {
                Username = "bob_student",
                Email = "bob@university.edu",
                PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("password123")),
                Role = UserRole.Student,
                FirstName = "Bob",
                LastName = "Smith",
                SchoolName = "University of Sydney",
                Bio = "Design enthusiast and photographer",
                IsVerified = true,
                HomeBaseLatitude = -33.8879,
                HomeBaseLongitude = 151.1876,
                HomeBaseAddress = "Camperdown NSW 2006",
                ReputationLevel = 0
            },
            new User
            {
                Username = "charlie_student",
                Email = "charlie@university.edu",
                PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("password123")),
                Role = UserRole.Student,
                FirstName = "Charlie",
                LastName = "Brown",
                SchoolName = "UNSW Sydney",
                Bio = "Engineering student, happy to help with math and tech",
                IsVerified = true,
                HomeBaseLatitude = -33.9173,
                HomeBaseLongitude = 151.2313,
                HomeBaseAddress = "Kensington NSW 2052",
                ReputationLevel = 0
            },
            new User
            {
                Username = "techcorp",
                Email = "hr@techcorp.com",
                PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("password123")),
                Role = UserRole.Company,
                CompanyName = "TechCorp Australia",
                WebsiteUrl = "https://techcorp.com.au",
                CompanyDescription = "Leading technology company in Sydney",
                HomeBaseLatitude = -33.8650,
                HomeBaseLongitude = 151.2094,
                HomeBaseAddress = "200 George St, Sydney NSW 2000"
            },
            new User
            {
                Username = "design_studio",
                Email = "contact@designstudio.com",
                PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("password123")),
                Role = UserRole.Company,
                CompanyName = "Creative Design Studio",
                WebsiteUrl = "https://designstudio.com",
                CompanyDescription = "Award-winning design agency",
                HomeBaseLatitude = -33.8697,
                HomeBaseLongitude = 151.2079,
                HomeBaseAddress = "Surry Hills NSW 2010"
            }
        };

        foreach (var userSeed in userSeeds)
        {
            if (!context.Users.Any(u => u.Username == userSeed.Username))
            {
                context.Users.Add(userSeed);
            }
        }

        context.SaveChanges();

        var trackedUsernames = userSeeds.Select(u => u.Username).ToList();
        var userLookup = context.Users
            .Where(u => trackedUsernames.Contains(u.Username))
            .ToDictionary(u => u.Username, u => u);

        if (!userLookup.TryGetValue("alice_student", out var alice) ||
            !userLookup.TryGetValue("bob_student", out var bob) ||
            !userLookup.TryGetValue("charlie_student", out var charlie) ||
            !userLookup.TryGetValue("techcorp", out var techcorp) ||
            !userLookup.TryGetValue("design_studio", out var designStudio))
        {
            return;
        }

        foreach (var user in userLookup.Values)
        {
            if (!context.Wallets.Any(w => w.UserId == user.Id))
            {
                context.Wallets.Add(new Wallet
                {
                    UserId = user.Id,
                    Balance = 1000
                });
            }
        }

        context.SaveChanges();

        var tasksToSeed = new List<SkillTask>
        {
            new SkillTask
            {
                Title = "Help with Python Assignment",
                Description = "Need help debugging a Python script for data analysis project",
                Category = TaskCategory.TechHelp,
                Status = SkillTaskStatus.Open,
                Budget = 50,
                Deadline = DateTime.UtcNow.AddDays(7),
                CreatorId = alice.Id,
                Latitude = -33.8836,
                Longitude = 151.2006,
                LocationAddress = "UTS Building 11"
            },
            new SkillTask
            {
                Title = "Photography for Event",
                Description = "Need a photographer for a student club event this weekend",
                Category = TaskCategory.PhotoVideo,
                Status = SkillTaskStatus.Open,
                Budget = 150,
                Deadline = DateTime.UtcNow.AddDays(3),
                CreatorId = bob.Id,
                Latitude = -33.8879,
                Longitude = 151.1876,
                LocationAddress = "University of Sydney, Quadrangle"
            },
            new SkillTask
            {
                Title = "Logo Design for Startup",
                Description = "Looking for a creative designer to create a modern logo",
                Category = TaskCategory.CreativeDesign,
                Status = SkillTaskStatus.Open,
                Budget = 200,
                Deadline = DateTime.UtcNow.AddDays(14),
                CreatorId = charlie.Id,
                Latitude = -33.9173,
                Longitude = 151.2313,
                LocationAddress = "UNSW Campus"
            },
            new SkillTask
            {
                Title = "Essay Proofreading",
                Description = "Need someone to proofread my 3000-word essay",
                Category = TaskCategory.WritingEditing,
                Status = SkillTaskStatus.Open,
                Budget = 80,
                Deadline = DateTime.UtcNow.AddDays(5),
                CreatorId = alice.Id,
                Latitude = -33.8836,
                Longitude = 151.2006,
                LocationAddress = "UTS Library"
            },
            new SkillTask
            {
                Title = "Spanish Language Tutor",
                Description = "Looking for a Spanish tutor for conversational practice",
                Category = TaskCategory.LanguageHelp,
                Status = SkillTaskStatus.Open,
                Budget = 40,
                Deadline = DateTime.UtcNow.AddDays(30),
                CreatorId = bob.Id,
                Latitude = -33.8879,
                Longitude = 151.1876,
                LocationAddress = "Sydney CBD"
            },
            new SkillTask
            {
                Title = "Math Exam Preparation",
                Description = "Need help preparing for calculus final exam",
                Category = TaskCategory.StudyHelp,
                Status = SkillTaskStatus.Assigned,
                Budget = 100,
                Deadline = DateTime.UtcNow.AddDays(10),
                CreatorId = alice.Id,
                AssignedToId = charlie.Id,
                Latitude = -33.8836,
                Longitude = 151.2006,
                LocationAddress = "UTS Building 2",
                IsDepositPaid = true,
                DepositAmount = 10
            }
        };

        foreach (var taskSeed in tasksToSeed)
        {
            if (!context.SkillTasks.Any(t => t.Title == taskSeed.Title))
            {
                context.SkillTasks.Add(taskSeed);
            }
        }

        context.SaveChanges();

        var taskTitles = tasksToSeed.Select(t => t.Title).ToList();
        var taskLookup = context.SkillTasks
            .Where(t => taskTitles.Contains(t.Title))
            .ToDictionary(t => t.Title, t => t);

        var jobsToSeed = new List<Job>
        {
            new Job
            {
                Title = "Software Engineering Intern",
                Responsibilities = "Develop web applications using React and Node.js. Work with senior developers on real projects.",
                Qualifications = "Currently studying Computer Science or related field. Familiar with JavaScript and web development.",
                EmploymentType = EmploymentType.Internship,
                PostedById = techcorp.Id,
                Latitude = -33.8650,
                Longitude = 151.2094,
                LocationAddress = "200 George St, Sydney NSW 2000",
                IsOpen = true
            },
            new Job
            {
                Title = "Graphic Design Intern",
                Responsibilities = "Create visual content for social media and marketing campaigns. Assist senior designers with projects.",
                Qualifications = "Portfolio showcasing design skills. Proficient in Adobe Creative Suite.",
                EmploymentType = EmploymentType.PartTime,
                PostedById = designStudio.Id,
                Latitude = -33.8697,
                Longitude = 151.2079,
                LocationAddress = "Surry Hills NSW 2010",
                IsOpen = true
            },
            new Job
            {
                Title = "UI/UX Designer",
                Responsibilities = "Design user interfaces for mobile and web applications. Conduct user research and testing.",
                Qualifications = "2+ years experience in UI/UX design. Strong portfolio required.",
                EmploymentType = EmploymentType.FullTime,
                PostedById = techcorp.Id,
                Latitude = -33.8650,
                Longitude = 151.2094,
                LocationAddress = "200 George St, Sydney NSW 2000",
                IsOpen = true
            }
        };

        foreach (var jobSeed in jobsToSeed)
        {
            if (!context.Jobs.Any(j => j.Title == jobSeed.Title))
            {
                context.Jobs.Add(jobSeed);
            }
        }

        context.SaveChanges();

        var coursesToSeed = new List<Course>
        {
            new Course
            {
                Title = "Introduction to Web Development",
                Description = "Learn HTML, CSS, and JavaScript to build your first responsive websites from scratch.",
                Category = TaskCategory.TechHelp,
                Type = CourseType.OnlineCourse,
                Duration = "6 weeks",
                Difficulty = DifficultyLevel.Beginner,
                InstructorName = "Dr. Sarah Tech",
                ImageUrl = "https://images.pexels.com/photos/270404/pexels-photo-270404.jpeg?auto=compress&cs=tinysrgb&w=800",
                ExternalUrl = "https://example.com/web-dev"
            },
            new Course
            {
                Title = "Data Science with Python",
                Description = "Analyse data, build models, and visualise insights using Python, pandas and scikit-learn.",
                Category = TaskCategory.TechHelp,
                Type = CourseType.WebSeminar,
                Duration = "8 weeks",
                Difficulty = DifficultyLevel.Intermediate,
                InstructorName = "Dr. Alan Wong",
                ImageUrl = "https://images.pexels.com/photos/1181671/pexels-photo-1181671.jpeg?auto=compress&cs=tinysrgb&w=800"
            },
            new Course
            {
                Title = "Graphic Design Fundamentals",
                Description = "Master design principles, colour theory and the Adobe Creative Suite.",
                Category = TaskCategory.CreativeDesign,
                Type = CourseType.OnlineCourse,
                Duration = "8 weeks",
                Difficulty = DifficultyLevel.Beginner,
                InstructorName = "Mark Designer",
                ImageUrl = "https://images.pexels.com/photos/196645/pexels-photo-196645.jpeg?auto=compress&cs=tinysrgb&w=800"
            },
            new Course
            {
                Title = "UI/UX Design Essentials",
                Description = "Design intuitive interfaces and prototype real products in Figma.",
                Category = TaskCategory.CreativeDesign,
                Type = CourseType.Workshop,
                Duration = "5 weeks",
                Difficulty = DifficultyLevel.Intermediate,
                InstructorName = "Lena Park",
                ImageUrl = "https://images.pexels.com/photos/196644/pexels-photo-196644.jpeg?auto=compress&cs=tinysrgb&w=800"
            },
            new Course
            {
                Title = "Photography Masterclass",
                Description = "Master composition, lighting and photo editing for stunning shots.",
                Category = TaskCategory.PhotoVideo,
                Type = CourseType.Workshop,
                Duration = "3 days",
                Difficulty = DifficultyLevel.Intermediate,
                InstructorName = "John Photographer",
                ImageUrl = "https://images.pexels.com/photos/1264210/pexels-photo-1264210.jpeg?auto=compress&cs=tinysrgb&w=800"
            },
            new Course
            {
                Title = "Video Editing & Production",
                Description = "Edit cinematic videos and tell stories with Premiere Pro and DaVinci Resolve.",
                Category = TaskCategory.PhotoVideo,
                Type = CourseType.OnlineCourse,
                Duration = "6 weeks",
                Difficulty = DifficultyLevel.Intermediate,
                InstructorName = "Carlos Vega",
                ImageUrl = "https://images.pexels.com/photos/7234256/pexels-photo-7234256.jpeg?auto=compress&cs=tinysrgb&w=800"
            },
            new Course
            {
                Title = "Creative Writing Workshop",
                Description = "Improve your storytelling, voice and editing across fiction and non-fiction.",
                Category = TaskCategory.WritingEditing,
                Type = CourseType.Workshop,
                Duration = "4 weeks",
                Difficulty = DifficultyLevel.Beginner,
                InstructorName = "Emma Writer",
                ImageUrl = "https://images.pexels.com/photos/590016/pexels-photo-590016.jpeg?auto=compress&cs=tinysrgb&w=800"
            },
            new Course
            {
                Title = "Copywriting for Marketing",
                Description = "Write persuasive copy for ads, landing pages and social campaigns.",
                Category = TaskCategory.WritingEditing,
                Type = CourseType.OnlineCourse,
                Duration = "5 weeks",
                Difficulty = DifficultyLevel.Intermediate,
                InstructorName = "Rachel Bloom",
                ImageUrl = "https://images.pexels.com/photos/905163/pexels-photo-905163.jpeg?auto=compress&cs=tinysrgb&w=800"
            },
            new Course
            {
                Title = "Spanish for Beginners",
                Description = "Start speaking Spanish with practical vocabulary and everyday conversation.",
                Category = TaskCategory.LanguageHelp,
                Type = CourseType.OnlineCourse,
                Duration = "10 weeks",
                Difficulty = DifficultyLevel.Beginner,
                InstructorName = "Maria Garcia",
                ImageUrl = "https://images.pexels.com/photos/256417/pexels-photo-256417.jpeg?auto=compress&cs=tinysrgb&w=800"
            },
            new Course
            {
                Title = "Business English & IELTS Prep",
                Description = "Sharpen academic and professional English and prepare for the IELTS exam.",
                Category = TaskCategory.LanguageHelp,
                Type = CourseType.WebSeminar,
                Duration = "7 weeks",
                Difficulty = DifficultyLevel.Intermediate,
                InstructorName = "James Carter",
                ImageUrl = "https://images.pexels.com/photos/267669/pexels-photo-267669.jpeg?auto=compress&cs=tinysrgb&w=800"
            },
            new Course
            {
                Title = "Advanced Mathematics Techniques",
                Description = "Master calculus and linear algebra with worked examples and problem sets.",
                Category = TaskCategory.StudyHelp,
                Type = CourseType.WebSeminar,
                Duration = "5 weeks",
                Difficulty = DifficultyLevel.Advanced,
                InstructorName = "Prof. David Math",
                ImageUrl = "https://images.pexels.com/photos/6256065/pexels-photo-6256065.jpeg?auto=compress&cs=tinysrgb&w=800"
            },
            new Course
            {
                Title = "Public Speaking & Presentation",
                Description = "Build confidence and deliver clear, compelling presentations to any audience.",
                Category = TaskCategory.StudyHelp,
                Type = CourseType.Workshop,
                Duration = "3 weeks",
                Difficulty = DifficultyLevel.Beginner,
                InstructorName = "Nina Roberts",
                ImageUrl = "https://images.pexels.com/photos/2173508/pexels-photo-2173508.jpeg?auto=compress&cs=tinysrgb&w=800"
            }
        };

        foreach (var courseSeed in coursesToSeed)
        {
            var existing = context.Courses.FirstOrDefault(c => c.Title == courseSeed.Title);
            if (existing == null)
            {
                context.Courses.Add(courseSeed);
            }
            else
            {
                // Refresh content/imagery for previously-seeded courses.
                existing.Description = courseSeed.Description;
                existing.ImageUrl = courseSeed.ImageUrl;
                existing.Category = courseSeed.Category;
                existing.Type = courseSeed.Type;
                existing.Duration = courseSeed.Duration;
                existing.Difficulty = courseSeed.Difficulty;
                existing.InstructorName = courseSeed.InstructorName;
            }
        }

        context.SaveChanges();

        var progressSeeds = new List<UserSkillProgress>
        {
            new UserSkillProgress
            {
                UserId = charlie.Id,
                Category = TaskCategory.StudyHelp,
                TotalXp = 850,
                CurrentTier = BadgeTier.Expert
            },
            new UserSkillProgress
            {
                UserId = charlie.Id,
                Category = TaskCategory.TechHelp,
                TotalXp = 450,
                CurrentTier = BadgeTier.Advanced
            }
        };

        foreach (var progressSeed in progressSeeds)
        {
            if (!context.UserSkillProgress.Any(p =>
                    p.UserId == progressSeed.UserId &&
                    p.Category == progressSeed.Category))
            {
                context.UserSkillProgress.Add(progressSeed);
            }
        }

        context.SaveChanges();

        var badgeSeeds = new List<UserBadge>
        {
            new UserBadge
            {
                UserId = charlie.Id,
                Category = TaskCategory.StudyHelp,
                Tier = BadgeTier.Expert
            },
            new UserBadge
            {
                UserId = charlie.Id,
                Category = TaskCategory.TechHelp,
                Tier = BadgeTier.Advanced
            }
        };

        foreach (var badgeSeed in badgeSeeds)
        {
            if (!context.UserBadges.Any(b =>
                    b.UserId == badgeSeed.UserId &&
                    b.Category == badgeSeed.Category &&
                    b.Tier == badgeSeed.Tier))
            {
                context.UserBadges.Add(badgeSeed);
            }
        }

        context.SaveChanges();

        if (!taskLookup.TryGetValue("Math Exam Preparation", out var mathPrepTask))
        {
            return;
        }

        var ratingSeeds = new List<Rating>
        {
            new Rating
            {
                FromUserId = alice.Id,
                ToUserId = charlie.Id,
                TaskId = mathPrepTask.Id,
                Category = TaskCategory.StudyHelp,
                Stars = 5,
                Comment = "Charlie is an amazing tutor! Very patient and knowledgeable.",
                XpAwarded = 12
            },
            new Rating
            {
                FromUserId = bob.Id,
                ToUserId = charlie.Id,
                Category = TaskCategory.TechHelp,
                Stars = 4,
                Comment = "Great help with my coding assignment. Would recommend!",
                XpAwarded = 10
            }
        };

        foreach (var ratingSeed in ratingSeeds)
        {
            if (!context.Ratings.Any(r => r.Comment == ratingSeed.Comment))
            {
                context.Ratings.Add(ratingSeed);
            }
        }

        context.SaveChanges();
    }
}
