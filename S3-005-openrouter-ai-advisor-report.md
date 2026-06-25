# S3-005 OpenRouter AI Advisor Improvement Report

## Branches Reviewed

- Current branch: `feature/S3-005-wire-openai-ai-advisor`
- Comparison branch: `origin/feature/S3-005-wire-gemini-ai-advisor-upgraded`
- Comparison branch: `origin/wire-Gemini-ai-advisor`

## Decision

Kept the current OpenRouter/OpenAI-compatible advisor stack. The Gemini branches replace the provider, delete the OpenRouter setup guide, and remove the richer `AiAdvisorContextDto` flow. I reused the durable ideas from those branches without switching providers.

## Improvements Applied

- Made OpenRouter the configured default provider:
  - `OpenAi:BaseUrl` now defaults to `https://openrouter.ai/api/v1`.
  - `OpenAi:Model` now defaults to `openrouter/free`.
  - The advisor UI now says `Powered by OpenRouter`.
- Added saved chat history:
  - New `AiAdvisorChatMessages` table stores student chat turns.
  - New `IAiAdvisorHistoryService` loads, saves, and clears history.
  - `/Student/AiAdvisorHistory` restores saved chat after refresh.
  - `/Student/AiAdvisorClearHistory` clears saved history with antiforgery protection.
  - Chat requests now use server-side saved history instead of trusting browser-provided history.
  - `OpenAiChatClient` sends messages as `system -> history -> current user`.
- Added comfortable follow-up prompts after each successful AI answer.
- Added lightweight formatting so advisor answers render bold/list text cleanly instead of showing raw markdown markers.
- Added provider payload normalization to unwrap common JSON `text`/`content` responses before rendering.
- Enriched the academic prompt with planning data inspired by the Gemini prompt:
  - minimum graduation GPA
  - credits remaining
  - overall progress percentage
  - overload eligibility
  - on-track status
  - available courses
  - locked courses
- Applied `AsSplitQuery()` to graduation-audit loading to address the EF multiple-collection include warning found in the old logs.
- Updated setup docs to describe OpenRouter while keeping the existing `OpenAi` configuration section for compatibility.
- Added/updated tests for context mapping, prompt contents, history loading/saving, provider payload normalization, and controller history endpoints.

## Ai_Advisor_Task Findings

- The task note requested a more comfortable UI, suggested questions after answers, better API context, and faster database queries.
- The screenshots showed:
  - a raw JSON/provider payload appearing in a chat bubble
  - markdown markers such as `**bold**` displaying literally
  - long answer blocks that made the page feel heavy
  - a useful error banner pattern worth keeping
  - an empty state and starting chips that already felt clear
- The logs showed:
  - `OpenAI returned an empty AI advisor response`
  - EF Core `MultipleCollectionIncludeWarning` on graduation-audit loading

## Bugs Found And Fixed

1. The advisor prompt did not include available or locked courses.
   - Impact: questions like "What should I take next semester?" could be answered with incomplete context.
   - Fix: mapped `CourseAuditStatus.Available` and `CourseAuditStatus.Locked` into the advisor context and prompt.

2. Follow-up chat messages had no conversational memory.
   - Impact: refreshes lost the conversation, and the provider only saw the latest message.
   - Fix: added database-backed per-student history and bounded provider history.

3. OpenRouter branch still had OpenAI-facing defaults and UI text.
   - Impact: local setup could silently fall back to OpenAI defaults unless secrets overrode every setting.
   - Fix: set safe tracked OpenRouter endpoint/model defaults and updated visible/provider-facing wording.

4. Conversation history needed server-side bounds.
   - Impact: a long or malformed client payload could make provider requests unnecessarily large.
   - Fix: provider history now comes from saved server data, and the advisor service still sanitizes roles/content before provider calls.

5. Raw provider payloads could appear in the UI.
   - Impact: users could see JSON-like payloads instead of a friendly answer.
   - Fix: normalized JSON `text`/`content` payloads in `AiAdvisorService`.

6. Advisor context loading triggered an EF multiple-collection include warning.
   - Impact: context queries could become slower as student records grow.
   - Fix: added `AsSplitQuery()` for the student audit and requirement queries.

## Intentionally Not Ported

- Gemini provider service, DTOs, and package references.
- Gemini controller response shape `{ reply }`, because the current `AiAdvisorResponseDto` success/failure contract is richer and already tested.
- Gemini view text with hardcoded student profile pills; the current branch already uses generic profile/audit/standing labels.
- Gemini client-side raw AI HTML rendering; the current branch already escapes AI text before adding course-code highlighting.

## Verification

Command run from `src/`:

```bash
dotnet test Cursus.sln
```

Command run from the repository root:

```bash
node --check src/Cursus.PL/wwwroot/js/pages/student/ai-advisor.js
```

Result:

- `Cursus.BLL.Tests`: 11 passed
- `Cursus.PL.Tests`: 8 passed
- Total: 19 passed
- Advisor JavaScript syntax check passed

Known warnings:

- `NU1901` low-severity advisory warnings for `NuGet.Packaging` and `NuGet.Protocol` version `6.12.1`.

## Notes

- No API key was added to tracked configuration.
- Existing untracked local files were left untouched.
