-- =============================================================================
-- Student 1 Database Microservice: Initial Schema & Seed Data
-- =============================================================================

CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260908024635_InitialCreate', '10.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;

-- -----------------------------------------------------------------------------
-- 1. Table: Notifications
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "Notifications" (
    "Id" uuid NOT NULL,
    "StudentId" uuid NOT NULL,
    "Type" text NOT NULL,
    "SourceMicroservice" text NOT NULL,
    "Message" text NOT NULL,
    "IsRead" boolean NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "RelatedEntityType" text,
    "RelatedEntityId" uuid,
    "ActionPayload" text,
    CONSTRAINT "PK_Notifications" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_Notifications_RelatedEntityType_RelatedEntityId"
    ON "Notifications" ("RelatedEntityType", "RelatedEntityId");

-- -----------------------------------------------------------------------------
-- 2. Table: NotificationPreferences
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "NotificationPreferences" (
    "Id" uuid NOT NULL,
    "StudentId" uuid NOT NULL,
    "Type" text NOT NULL,
    "Channel" text NOT NULL,
    "Enabled" boolean NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_NotificationPreferences" PRIMARY KEY ("Id")
);

-- -----------------------------------------------------------------------------
-- 3. Table: AiDigests
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "AiDigests" (
    "Id" uuid NOT NULL,
    "StudentId" uuid NOT NULL,
    "Summary" text NOT NULL,
    "GeneratedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_AiDigests" PRIMARY KEY ("Id")
);

-- -----------------------------------------------------------------------------
-- 4. Table: CanvasAssignmentWatermarks
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "CanvasAssignmentWatermarks" (
    "Id" uuid NOT NULL,
    "CanvasAssignmentId" bigint NOT NULL,
    "LastDueDate" timestamp with time zone,
    "LastWorkflowState" text,
    "LastSubmissionState" text,
    "LastSeenAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_CanvasAssignmentWatermarks" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_CanvasAssignmentWatermarks_CanvasAssignmentId"
    ON "CanvasAssignmentWatermarks" ("CanvasAssignmentId");

-- -----------------------------------------------------------------------------
-- Seed Data (Minimum 10 records per table)
-- -----------------------------------------------------------------------------

-- Notifications (11 records)
INSERT INTO "Notifications" ("Id", "StudentId", "Type", "SourceMicroservice", "Message", "IsRead", "CreatedAtUtc", "RelatedEntityType", "RelatedEntityId", "ActionPayload")
VALUES
    ('a0000001-0000-0000-0000-000000000001', '11111111-1111-1111-1111-111111111111', 'Deadline', 'deadline-tracker', 'Your assignment is due soon.', false, NOW() - INTERVAL '1 hour', 'Task', '33333333-3333-3333-3333-333333333331', NULL),
    ('a0000001-0000-0000-0000-000000000002', '11111111-1111-1111-1111-111111111111', 'Grade', 'grading-service', 'A new grade has been posted.', false, NOW() - INTERVAL '2 hours', 'Grade', '55555555-5555-5555-5555-555555555551', NULL),
    ('a0000001-0000-0000-0000-000000000003', '11111111-1111-1111-1111-111111111111', 'Automation', 'automation-engine', 'Automation run completed successfully.', true, NOW() - INTERVAL '3 hours', NULL, NULL, NULL),
    ('a0000001-0000-0000-0000-000000000004', '11111111-1111-1111-1111-111111111111', 'Account', 'identity-service', 'Your account settings were updated.', true, NOW() - INTERVAL '4 hours', NULL, NULL, NULL),
    ('a0000001-0000-0000-0000-000000000005', '11111111-1111-1111-1111-111111111111', 'AI', 'ai-digest-service', 'Your weekly AI digest is ready.', false, NOW() - INTERVAL '5 hours', NULL, NULL, NULL),
    ('a0000001-0000-0000-0000-000000000006', '11111111-1111-1111-1111-111111111111', 'Deadline', 'deadline-tracker', 'A task deadline is approaching.', false, NOW() - INTERVAL '6 hours', 'Task', '33333333-3333-3333-3333-333333333332', NULL),
    ('a0000001-0000-0000-0000-000000000007', '22222222-2222-2222-2222-222222222222', 'Grade', 'grading-service', 'A new grade has been posted.', false, NOW() - INTERVAL '1 hour', 'Grade', '55555555-5555-5555-5555-555555555552', NULL),
    ('a0000001-0000-0000-0000-000000000008', '22222222-2222-2222-2222-222222222222', 'Automation', 'automation-engine', 'Automation flagged a task for follow-up.', false, NOW() - INTERVAL '2 hours', NULL, NULL, NULL),
    ('a0000001-0000-0000-0000-000000000009', '22222222-2222-2222-2222-222222222222', 'Account', 'identity-service', 'Your account settings were updated.', true, NOW() - INTERVAL '3 hours', NULL, NULL, NULL),
    ('a0000001-0000-0000-0000-000000000010', '22222222-2222-2222-2222-222222222222', 'AI', 'ai-digest-service', 'Your weekly AI digest is ready.', true, NOW() - INTERVAL '4 hours', NULL, NULL, NULL),
    ('a0000001-0000-0000-0000-000000000011', '22222222-2222-2222-2222-222222222222', 'Deadline', 'deadline-tracker', 'A task deadline is approaching.', false, NOW() - INTERVAL '5 hours', 'Task', '33333333-3333-3333-3333-333333333333', NULL)
ON CONFLICT ("Id") DO NOTHING;

-- NotificationPreferences (11 records)
INSERT INTO "NotificationPreferences" ("Id", "StudentId", "Type", "Channel", "Enabled", "UpdatedAtUtc")
VALUES
    ('b0000001-0000-0000-0000-000000000001', '11111111-1111-1111-1111-111111111111', 'Deadline', 'InApp', true, NOW()),
    ('b0000001-0000-0000-0000-000000000002', '11111111-1111-1111-1111-111111111111', 'Deadline', 'Email', true, NOW()),
    ('b0000001-0000-0000-0000-000000000003', '11111111-1111-1111-1111-111111111111', 'Grade', 'InApp', true, NOW()),
    ('b0000001-0000-0000-0000-000000000004', '11111111-1111-1111-1111-111111111111', 'Grade', 'Email', false, NOW()),
    ('b0000001-0000-0000-0000-000000000005', '11111111-1111-1111-1111-111111111111', 'Automation', 'InApp', false, NOW()),
    ('b0000001-0000-0000-0000-000000000006', '11111111-1111-1111-1111-111111111111', 'Account', 'Email', true, NOW()),
    ('b0000001-0000-0000-0000-000000000007', '11111111-1111-1111-1111-111111111111', 'AI', 'InApp', true, NOW()),
    ('b0000001-0000-0000-0000-000000000008', '22222222-2222-2222-2222-222222222222', 'Deadline', 'InApp', true, NOW()),
    ('b0000001-0000-0000-0000-000000000009', '22222222-2222-2222-2222-222222222222', 'Grade', 'Email', true, NOW()),
    ('b0000001-0000-0000-0000-000000000010', '22222222-2222-2222-2222-222222222222', 'Automation', 'Email', false, NOW()),
    ('b0000001-0000-0000-0000-000000000011', '22222222-2222-2222-2222-222222222222', 'AI', 'InApp', false, NOW())
ON CONFLICT ("Id") DO NOTHING;

-- AiDigests (10 records)
INSERT INTO "AiDigests" ("Id", "StudentId", "Summary", "GeneratedAtUtc")
VALUES
    ('c0000001-0000-0000-0000-000000000001', '11111111-1111-1111-1111-111111111111', 'You have 2 deadlines due this week and 1 unread grade.', NOW() - INTERVAL '1 day'),
    ('c0000001-0000-0000-0000-000000000002', '11111111-1111-1111-1111-111111111111', 'Automation run completed successfully for your enrolled courses.', NOW() - INTERVAL '2 days'),
    ('c0000001-0000-0000-0000-000000000003', '11111111-1111-1111-1111-111111111111', 'Account settings were updated; review recent activity.', NOW() - INTERVAL '3 days'),
    ('c0000001-0000-0000-0000-000000000004', '11111111-1111-1111-1111-111111111111', '3 tasks completed this week, on track for the sprint.', NOW() - INTERVAL '4 days'),
    ('c0000001-0000-0000-0000-000000000005', '11111111-1111-1111-1111-111111111111', 'New grade posted for Advanced Software Development.', NOW() - INTERVAL '5 days'),
    ('c0000001-0000-0000-0000-000000000006', '22222222-2222-2222-2222-222222222222', '1 deadline overdue, consider rescheduling your study plan.', NOW() - INTERVAL '1 day'),
    ('c0000001-0000-0000-0000-000000000007', '22222222-2222-2222-2222-222222222222', 'Weekly summary: 5 notifications, 2 unread.', NOW() - INTERVAL '2 days'),
    ('c0000001-0000-0000-0000-000000000008', '22222222-2222-2222-2222-222222222222', 'Automation engine flagged a task for follow-up.', NOW() - INTERVAL '3 days'),
    ('c0000001-0000-0000-0000-000000000009', '22222222-2222-2222-2222-222222222222', 'No new grades this week.', NOW() - INTERVAL '4 days'),
    ('c0000001-0000-0000-0000-000000000010', '22222222-2222-2222-2222-222222222222', 'Digest generated: all systems normal.', NOW() - INTERVAL '5 days')
ON CONFLICT ("Id") DO NOTHING;

-- CanvasAssignmentWatermarks (10 records)
INSERT INTO "CanvasAssignmentWatermarks" ("Id", "CanvasAssignmentId", "LastDueDate", "LastWorkflowState", "LastSubmissionState", "LastSeenAtUtc")
VALUES
    ('d0000001-0000-0000-0000-000000000001', 101001, NOW() + INTERVAL '3 days', 'published', 'unsubmitted', NOW()),
    ('d0000001-0000-0000-0000-000000000002', 101002, NOW() + INTERVAL '5 days', 'published', 'submitted', NOW()),
    ('d0000001-0000-0000-0000-000000000003', 101003, NOW() + INTERVAL '7 days', 'published', 'unsubmitted', NOW()),
    ('d0000001-0000-0000-0000-000000000004', 101004, NOW() + INTERVAL '10 days', 'published', 'unsubmitted', NOW()),
    ('d0000001-0000-0000-0000-000000000005', 101005, NOW() - INTERVAL '1 day', 'published', 'graded', NOW()),
    ('d0000001-0000-0000-0000-000000000006', 101006, NOW() + INTERVAL '12 days', 'published', 'unsubmitted', NOW()),
    ('d0000001-0000-0000-0000-000000000007', 101007, NOW() + INTERVAL '14 days', 'published', 'unsubmitted', NOW()),
    ('d0000001-0000-0000-0000-000000000008', 101008, NOW() + INTERVAL '18 days', 'published', 'unsubmitted', NOW()),
    ('d0000001-0000-0000-0000-000000000009', 101009, NOW() - INTERVAL '2 days', 'published', 'graded', NOW()),
    ('d0000001-0000-0000-0000-000000000010', 101010, NOW() + INTERVAL '21 days', 'published', 'unsubmitted', NOW())
ON CONFLICT ("CanvasAssignmentId") DO NOTHING;
