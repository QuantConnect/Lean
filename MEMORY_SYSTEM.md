# Memory System - Foolproof Context Preservation

> **5 layers of redundancy. Paranoia-grade memory.**

---

## Layer 1: CLAUDE.md (Project Memory)

**What:** AI-readable project context file
**When:** Read at every session start
**Location:** `/Users/junaidhassan/Lean/CLAUDE.md`

**Update triggers:**
- Major decisions made
- Strategy parameters changed
- New blockers discovered
- Phase transitions

---

## Layer 2: SESSION_LOG.md (Running Activity Log)

**What:** Append-only log of session activities
**When:** Updated throughout session
**Location:** `/Users/junaidhassan/Lean/SESSION_LOG.md`

**Format:**
```
## YYYY-MM-DD HH:MM - Session Start
- Context: What we're working on
- Goals: What we aim to accomplish

### Activity Log
- [HH:MM] Did X
- [HH:MM] Discovered Y
- [HH:MM] Decision: Z

### Session End Summary
- Completed: A, B, C
- Pending: D, E
- Blockers: F
```

---

## Layer 3: Episodic Memory (Cross-Session Search)

**What:** Automatic conversation archiving + semantic search
**When:** Auto-syncs at session start
**Location:** `~/.config/superpowers/conversations.db`

**How to search:**
```bash
# From terminal
episodic-memory search "ROC calculation"
episodic-memory search --after 2024-12-01 "straddle"

# From Claude Code
/episodic-memory:search-conversations "what we decided about IVR"
```

**Already installed and working.**

---

## Layer 4: Git Checkpoints

**What:** Automatic commits at milestones
**When:** After significant progress

**Checkpoint script:** `/Users/junaidhassan/Lean/checkpoint.sh`

```bash
./checkpoint.sh "Completed backtest analysis"
```

**Auto-commits:**
- All changed files in Lean directory
- Timestamped commit message
- Never pushes (manual push only)

---

## Layer 5: Context Alert System

**What:** Warning when context is running low
**When:** At 60%, 40%, 20% context remaining

**Action at each threshold:**
- 60%: Reminder to update CLAUDE.md if needed
- 40%: Auto-update SESSION_LOG.md with current state
- 20%: Final checkpoint, prepare for handoff

---

## Quick Commands

| Command | What It Does |
|---------|--------------|
| `./checkpoint.sh "message"` | Git commit all changes |
| `/episodic-memory:search-conversations "query"` | Search past sessions |
| `episodic-memory stats` | See how many sessions indexed |

---

## Recovery Procedure

If session ends unexpectedly:

1. **Read CLAUDE.md** - Get project context
2. **Read SESSION_LOG.md** - See recent activity
3. **Search episodic memory** - Find specific discussions
4. **Check git log** - See what was committed

```bash
# Quick recovery
cat CLAUDE.md
tail -100 SESSION_LOG.md
git log --oneline -10
episodic-memory search "last thing we worked on"
```

---

## Setup Checklist

- [x] CLAUDE.md exists and is current
- [x] Episodic memory plugin installed
- [ ] SESSION_LOG.md created
- [ ] checkpoint.sh script created
- [ ] First checkpoint committed

