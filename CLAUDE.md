# Claude Code Instructions

This file contains all instructions and workflows for Claude Code sessions in this repository.

## Core Responsibilities

### Branch Consistency Management
- **Monitor feature completion**: When a feature appears to be complete, ask: "It looks like we're done with this feature. Should I sync the branches and shuttle commits to your primary working area?"
- **Branch operations require approval**: Never perform git merge, rebase, or push operations without explicit user permission
- **Sync workflow**: When approved, help shuttle commits between worktree branches and primary branches (typically `cve-json`)
- **Keep branches consistent**: After shuttling commits, ensure worktree branches are updated to match the primary branch

### Feature Completion Indicators
- All compilation errors resolved
- Tests passing (if applicable)
- User indicates work is complete
- No active development tasks remain
- Clean working directory

## Git Worktree Workflow

### Directory Structure
```
repo-root/
├── _worktree/           # Git-ignored directory for all worktrees  
│   ├── feature-branch/  # Individual worktree directories
│   ├── bugfix-123/
│   └── experiment/
├── .gitignore           # Contains "_worktree/" entry
├── CLAUDE.md            # This file - primary instructions
└── ... (main repo files)
```

### Session Start Protocol
- **Always start CC sessions at repo root**
- If CC detects we're in main worktree area (not in `_worktree/`):
  1. Pause and ask: "You're in the main area. Would you like me to create/switch to a worktree?"
  2. If yes, prompt for worktree name
  3. Create worktree in `_worktree/{name}` and switch to it
  4. If no, continue but remind about worktree best practices

### Worktree Management Commands

#### Create New Worktree
```bash
# From repo root
git worktree add _worktree/{feature-name} {feature-name}
cd _worktree/{feature-name}
```

#### List Worktrees
```bash
git worktree list
```

#### Remove Worktree
```bash
# From repo root
git worktree remove _worktree/{feature-name}
# Or if directory deleted manually:
git worktree prune
```

### Branch Management

#### Shuttle Commits Between Branches (WITH USER APPROVAL)
```bash
# Go to primary branch location
cd /path/to/primary/branch
git merge {feature-branch}
git push origin {primary-branch}

# Return to worktree and sync
cd _worktree/{feature-name}
git merge origin/{primary-branch}
```

#### Update from Main
```bash
# From within worktree
git fetch origin
git rebase origin/main
```

### Claude Code Automation

#### Auto-Setup .gitignore
If `_worktree/` not in .gitignore:
1. Add `_worktree/` to .gitignore
2. Commit the change: "Add _worktree directory to gitignore"

#### Smart Working Directory Detection
- **In repo root**: Offer to create/switch to worktree
- **In `_worktree/{name}`**: Show current worktree status
- **Elsewhere**: Navigate back to appropriate location

#### File Editing Hygiene
When working in a worktree:
- **ALWAYS verify file paths before editing** - Ensure paths start with `_worktree/{name}/`
- **Example CORRECT path**: `/Users/rich/git/distroessed/_worktree/feature-name/src/DotnetRelease/DataModel/HalJson/Hal.cs`
- **Example WRONG path**: `/Users/rich/git/distroessed/src/DotnetRelease/DataModel/HalJson/Hal.cs`
- **Before any file edit**, confirm you're editing within the worktree, not the main repository
- **If you accidentally edit main repository files**, stop immediately and inform the user

### Safety Checks
- **Always ask before branch operations**: merges, rebases, pushes
- Warn before switching with uncommitted changes
- Confirm before removing worktrees
- Verify branch status before major operations
- **CRITICAL: Only edit files within the active worktree directory** - Never edit files in the main repository when working in a worktree

## Project-Specific Context

### Primary Working Branch
- Main development happens on `cve-json` branch
- Worktrees are created for isolated feature development
- Completed features are shuttled back to `cve-json`

### Common Workflows

#### Starting New Feature
1. Create worktree: `git worktree add _worktree/feature-name -b feature-name`
2. Work in isolation in `_worktree/feature-name/`
3. Regular commits and development

#### Completing Feature
1. **Ask user**: "Feature appears complete. Should I shuttle commits to cve-json?"
2. **If approved**: Merge commits to primary branch
3. **Sync worktree**: Update worktree branch to match primary
4. **Push changes**: Update remote branches

#### Context Switching
1. From any worktree: `cd ../../_worktree/other-feature/`
2. Update context automatically
3. Continue work in new context

### Required Prompts
- **Branch Operations**: "This will modify branches. Should I proceed?"
- **Feature Completion**: "It looks like we're done with this feature. Should I sync the branches?"
- **Location Check**: "I notice you're in {location}. Would you like to work in a worktree instead?"
- **Status Updates**: "Current worktree: {name} | Branch: {branch} | Status: {clean/dirty}"

## Quick Reference

### Essential Commands
```bash
# Create worktree
git worktree add _worktree/{feature-name} -b {feature-name}

# List worktrees  
git worktree list

# Remove worktree
git worktree remove _worktree/{feature-name}

# Shuttle commits (with approval)
cd /path/to/primary && git merge {feature-branch} && git push
cd _worktree/{feature-name} && git merge origin/{primary-branch}
```

### Core Principles
- **Always ask before git operations that affect branches**
- **Proactively offer to sync branches when features are complete**
- **Maintain consistency between worktree and primary branches**
- **Use `_worktree/` for all isolated work**
- **Let Claude manage worktree lifecycle with user approval**