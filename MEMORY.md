# Memory

## Perpetual Motion Mode (永動機模式)
- **Target**: Collaborator agent `skihelp`.
- **Logic**: Periodically check for open issues assigned to `skihelp` across all active repositories (`milk-api-manager-system`, `enterprise-ai-knowledge-integration`, `milk-ai-mcp-insuretech`, `enterprise-k8s-architecture-research`).
- **Trigger**: If `skihelp` has fewer than 2 active tasks, proactively create or assign new tasks from the project roadmap or backlog.
- **Goal**: Ensure `skihelp` never stays idle.

## Silent Replies
When you have nothing to say, respond with ONLY: NO_REPLY
⚠️ Rules:
- It must be your ENTIRE message — nothing else
- Never append it to an actual response (never include "NO_REPLY" in real replies)
- Never wrap it in markdown or code blocks
❌ Wrong: "Here's help... NO_REPLY"
❌ Wrong: "NO_REPLY"
✅ Right: NO_REPLY

## Heartbeats
Heartbeat prompt: Read HEARTBEAT.md if it exists (workspace context). Follow it strictly. Do not infer or repeat old tasks from prior chats. If nothing needs attention, reply HEARTBEAT_OK.
If you receive a heartbeat poll (a user message matching the heartbeat prompt above), and there is nothing that needs attention, reply exactly:
HEARTBEAT_OK
OpenClaw treats a leading/trailing "HEARTBEAT_OK" as a heartbeat ack (and may discard it).
If something needs attention, do NOT include "HEARTBEAT_OK"; reply with the alert text instead.

## Development Flow & Stability (穩定開發流程)
- **Mandatory Verification**: BEFORE any `git push` or concluding a task, you MUST run `./scripts/verify-all.sh` (Linux) or `./scripts/verify-all.ps1` (Windows).
- **Zero-Failure Policy**: Never commit or push if any test in the verification suite fails.
- **Reporting**: Always check `E2E_TEST_REPORT.md` after running the verification script to confirm all components are green.
- **Code Style**: Adhere to existing .NET naming conventions and ensure PII masking logic is covered by at least one E2E test.

## Multi-VPS & Human Collaboration (人機多節點協作規範)
- **Central Repository**: Both VPS must use `tedtv1007-ctrl/milk-api-manager-system` as the main remote `origin`.
- **Concurrency Locking**:
    1. START: Run `git pull origin main` and check `HEARTBEAT.md`.
    2. LOCK: If `USER_ACTIVE` is not "None", do NOT modify code. Only perform research or wait.
    3. CLAIM: If clear, update `HEARTBEAT.md` with your ID (e.g. `VPS_A: Working on Issue X`) before starting.
- **Verification Hierarchy**:
    - **Local/Human**: Must pass `./scripts/verify-all.ps1` (4 tests) before pushing.
    - **Zeabur/VPS**: Must pass `dotnet test` (Step 2), then push and monitor GitHub CI.
