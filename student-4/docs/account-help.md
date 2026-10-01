# Student 4 Account Help

## Login and registration

Open `/account/` to log in with your account email and password. An incorrect
email or password produces an invalid-credentials message. Registration
collects an email, password with confirmation, first and last names, date of
birth, and account role. Middle names are optional. Student and Teacher
registrations also collect the matching role details. After registration or
login the browser opens `/account/profile`.

## Editing a profile and role details

On the profile page select EDIT PROFILE, update your details, then select
SAVE CHANGES. CANCEL discards unsaved form changes. You can edit your email,
names, gender, date of birth, and profile summary. Student details include
FullTime, PartTime, or Inactive course status. Teacher details include
FullTime, PartTime, or Inactive employment status. Editing role details does
not change the account from Student to Teacher or Admin. Contact an
administrator if your matching student or teacher record is missing.

## Generating and saving an AI profile summary

GENERATE AI SUMMARY offers a new summary alongside the existing saved
summary. Choose which summary to use, then save your profile to apply it.
Generating or selecting a summary does not automatically persist it.
You can write a summary manually when AI Mode is unavailable.

## Changing a known password

Use CHANGE PASSWORD on the profile page when you know your current
password. Enter the current password, a new password, and a matching
confirmation. The profile form requires the new password to contain at
least eight characters. An incorrect current password prevents the change.
Do not submit passwords to Account Help, RAG questions, or MCP tools.

## Forgot password and reset email

On the login page select the forgot-password option and enter the account
email address. The authentication service responds with "If that email is
registered, a reset link has been sent." This generic message does not
prove that the account exists or that email delivery succeeded.
In local development reset messages go to MailHog at
`http://localhost:8025`. Check that inbox for the reset link.
If delivery fails, ask the project maintainer to check the authentication
service and SMTP configuration instead of repeatedly changing your profile.

## Resetting a password from an email link

Open the reset link from the email. The reset page reads its token from the
URL and asks for a new password and matching confirmation. A new reset
password must contain at least eight characters. A missing token requires
requesting a new link from the login page. Invalid, expired, or already-used
reset tokens are rejected; request a new reset email. Successful reset
returns you to login. Never paste reset links or tokens into Account Help.

## Deleting an account

DELETE ACCOUNT on the profile page requires your current password.
An incorrect password prevents deletion. Successful deletion returns the
browser to the login page. Account Help only explains the procedure; it
cannot delete an account or confirm whether an account has been deleted.

## Account readiness checks through MCP

Open the 03 KNOWLEDGE tab at `/account/knowledge`. CHECK MY ACCOUNT SETUP evaluates saved profile completeness, role setup,
Canvas gateway connectivity, and notification-service readiness. Missing
profile fields, a missing role record, or an inactive role status result in
NEEDS ATTENTION. Failed Canvas or notification probes result in UNAVAILABLE,
while other findings remain visible. EDIT PROFILE and REVIEW ROLE SETTINGS
return to 02 PROFILE in edit mode where appropriate. Save changes before checking again.
An inactive role finding is advisory and does not automatically change
account permissions or block existing features.

## Canvas and notification findings

Canvas connectivity is checked only through shared-backend's course API,
not directly against Canvas from the account service. A successful course
response may be cached and does not verify your personal enrolments.
When Canvas is unavailable, ask the maintainer to check the shared gateway
and its configured credentials. Notification readiness checks the
notification service's health, not your email delivery or preferences.
If account alerts are unavailable, start the notification service and retry.

## Grounded Account Help through RAG

Open 03 KNOWLEDGE at `/account/knowledge` to use Account Help. It answers general questions using retrieved account documentation.
It shows source citations and a low, medium, or high retrieval-based
confidence category. Confidence is not a guarantee of answer correctness.
Questions with no relevant account documentation receive an
insufficient-context response instead of an invented answer.
Account Help cannot inspect a specific user's account, verify email delivery,
retrieve a password, change settings, or perform account actions.
Use MCP for live readiness checks and the normal profile/authentication
forms for account actions. Never include passwords, tokens, API keys, or
other sensitive account information in a RAG question.
