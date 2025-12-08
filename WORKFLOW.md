# WORKFLOW.md - How We Work Together

> **This document defines how Junaid and AI collaborate on this project.**

---

## The Core Agreement

| Junaid Does | AI Does |
|-------------|---------|
| Describes intent in plain English | Translates to code |
| Validates logic matches intent | Handles all technical details |
| Reviews results and data | Maintains documentation |
| Makes trading decisions | Surfaces issues as decisions |
| NEVER debugs code | Explains what was built |

---

## Communication Principles

### From Junaid to AI

**Good:**
- "I want to test if adding a VIX filter improves win rate"
- "The exit at -3% from peak isn't working - trades are exiting too early"
- "Show me which trades would have been filtered out by IVR > 50"

**Not Needed:**
- Technical implementation details
- Code syntax or structure preferences
- Debugging information

### From AI to Junaid

**Good:**
- "I've added the VIX filter. Here's what changed: [table of impact]"
- "The -3% exit triggered on 23 trades. 15 of them recovered to +10% after. Should we widen to -5%?"
- Tables, comparisons, clear recommendations

**Avoid:**
- Technical jargon without explanation
- Code dumps
- "It's done" without showing impact

---

## Session Flow

### Starting a Session

```
1. AI reads CLAUDE.md (automatic)
2. AI checks current todos and project state
3. Junaid states what he wants to work on
4. AI confirms understanding, proposes approach
5. Work begins
```

### During a Session

```
1. Junaid describes intent
2. AI implements
3. AI shows results (tables, summaries, not code)
4. Junaid validates or requests changes
5. Repeat
```

### Ending a Session

```
1. AI updates CLAUDE.md with significant changes
2. AI updates todo list
3. AI summarizes what was accomplished
4. Any decisions/learnings captured in Notes section
```

---

## How to Request Changes

### Strategy Logic Changes

**Say:** "Change the entry rule: instead of IVR > 40, use IVR > 50"

**AI will:**
1. Update STRATEGY.md
2. Update config.json
3. Update algorithm.py
4. Confirm the change and show what's different

### "What If" Analysis

**Say:** "What if we used a -5% stop instead of -3%?"

**AI will:**
1. Run analysis on historical trades
2. Show comparison table (old vs new)
3. Recommend whether to make the change

### New Feature

**Say:** "I want to see which day of the week has the best win rate"

**AI will:**
1. Add the analysis capability
2. Run it on current data
3. Show results in a clear table

---

## Decision Framework

When AI encounters ambiguity, present it as a decision:

```
DECISION NEEDED: [Topic]

Option A: [Description]
- Pros: [list]
- Cons: [list]

Option B: [Description]
- Pros: [list]
- Cons: [list]

My recommendation: [Option X] because [reason]

Which would you prefer?
```

---

## File Modification Rules

### AI Can Freely Modify
- `algorithm.py` (implementation)
- `config.json` (parameters)
- `_analytics/*.py` (analysis tools)
- Database contents

### AI Updates After Changes
- `STRATEGY.md` (keep in sync with code)
- `CHANGELOG.md` (log what changed)
- `CLAUDE.md` (significant decisions only)

### AI Asks Before Modifying
- Core hypothesis or strategy direction
- Risk parameters (max loss, position sizing caps)
- Going live or changing live settings

---

## Running Backtests

### Junaid Says
"Run a backtest on the current strategy"

### AI Does
1. Ensures config.json is correct
2. Updates Launcher/config.json to point to algorithm
3. Runs the backtest
4. Parses results
5. Presents summary table
6. Stores results in `backtests/YYYY-MM-DD/`

### Output Format
```
BACKTEST RESULTS: [Strategy Name]
Period: [Start] to [End]

SUMMARY
───────
Total Return: XX%
Sharpe Ratio: X.XX
Max Drawdown: XX%
Win Rate: XX%
Total Trades: N

TOP 5 WINNERS
─────────────
[Table]

TOP 5 LOSERS
────────────
[Table]

MONTHLY BREAKDOWN
─────────────────
[Table]

Full results saved to: backtests/YYYY-MM-DD/
```

---

## Error Handling

### If Something Breaks

**AI should NOT say:**
- "There's a TypeError on line 47"
- "The DataFrame index is misaligned"
- "Here's the stack trace..."

**AI SHOULD say:**
- "The backtest failed because [plain English reason]"
- "I need to fix [what]. This will take [action]. Proceed?"
- "There's a data issue: [specific problem]. Options: A) [fix], B) [workaround]"

### Junaid Never Needs To
- Read error messages
- Understand stack traces
- Debug code
- Figure out file paths

---

## Progress Tracking

### Todo List Usage

AI maintains the todo list to show:
1. What we're currently working on
2. What's coming next
3. What's been completed

### Updating CLAUDE.md

Update the Notes & Decisions section when:
- A significant decision is made
- A key insight is discovered
- The strategy direction changes
- Something important is learned

---

## Quality Standards

### Every Strategy Must Have
- [ ] STRATEGY.md fully filled out
- [ ] config.json with all parameters
- [ ] algorithm.py that matches the spec
- [ ] At least one backtest run
- [ ] Results stored properly

### Before Going Live
- [ ] Paper traded for minimum period
- [ ] Out-of-sample validation
- [ ] Risk parameters reviewed
- [ ] Junaid explicitly approves

---

## Emergency Procedures

### Strategy Losing Money Live

1. AI surfaces immediately: "Live strategy [X] down [Y]%. Action needed?"
2. Options presented: Pause, reduce size, exit all, continue
3. Junaid decides
4. AI implements immediately

### Data Issues

1. AI identifies: "Data looks wrong: [observation]"
2. AI pauses any actions that depend on bad data
3. Options presented to fix or work around
4. Junaid decides

---

## Session Memory

### What Persists Between Sessions
- `CLAUDE.md` - Project state and context
- `STRATEGY.md` - Strategy specifications
- `config.json` - Parameters
- Database - All trade data
- `CHANGELOG.md` - History

### What Doesn't Persist
- Conversation details
- Intermediate analysis
- Temporary files

**Important:** If something should be remembered, it goes in a file.

---

*Last updated: 2024-12-09*
