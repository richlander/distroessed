# Claude Code Git Worktree Workflow

## Overview
This document defines the preferred git worktree workflow for Claude Code sessions. The goal is to isolate work in separate worktrees while maintaining easy movement between them.

## Directory Structure
```
repo-root/
├── _worktree/           # Git-ignored directory for all worktrees
│   ├── feature-branch/  # Individual worktree directories
│   ├── bugfix-123/
│   └── experiment/
├── .gitignore           # Contains "_worktree/" entry
└── ... (main repo files)
```

## Workflow Rules for Claude Code

### 1. Session Start Protocol
- **Always start CC sessions at repo root**
- If CC detects we're in main worktree area (not in `_worktree/`), it should:
  1. Pause and ask: "You're in the main area. Would you like me to create/switch to a worktree?"
  2. If yes, prompt for worktree name
  3. Create worktree in `_worktree/{name}` and switch to it
  4. If no, continue but remind about worktree best practices

### 2. Worktree Management Commands

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

#### Switch Between Worktrees
```bash
# Move to different worktree
cd _worktree/{other-feature}
# Return to main
cd ../../
```

### 3. Branch Management

#### Create Feature Branch in Worktree
```bash
# Create new branch from main and worktree
git worktree add _worktree/{feature-name} -b {feature-name}
cd _worktree/{feature-name}
```

#### Update from Main
```bash
# From within worktree
git fetch origin
git rebase origin/main
# If conflicts occur, resolve them and continue:
# git add <resolved-files>
# git rebase --continue
```

#### Set Up Remote Tracking
```bash
# After creating first commit in worktree
git push -u origin {feature-name}
# Subsequent pushes can use:
git push
```

### 4. Claude Code Automation

#### Auto-Setup .gitignore
If `_worktree/` not in .gitignore, CC should:
1. Add `_worktree/` to .gitignore
2. Commit the change: "Add _worktree directory to gitignore"

#### Smart Working Directory Detection
CC should detect current location and suggest actions:
- **In repo root**: Offer to create/switch to worktree
- **In `_worktree/{name}`**: Show current worktree status
- **Elsewhere**: Navigate back to appropriate location

#### Worktree Status Helper
CC should provide a status command showing:
- Current worktree location
- Available worktrees
- Branch status
- Uncommitted changes

### 5. Best Practices

#### Worktree Naming
- Use descriptive names: `feature-auth`, `bugfix-login`, `experiment-perf`
- Avoid spaces, use hyphens
- Keep names concise but clear

#### Branch Lifecycle
1. Create worktree with new branch
2. Work in isolation
3. Regular commits in worktree
4. Set up remote tracking: `git push -u origin {feature-name}`
5. When ready: push branch, create PR
6. After merge: remove worktree and delete remote branch

#### Commit Management
- Make frequent small commits in worktree
- Use interactive rebase to clean up before pushing
- Consider squashing related commits

### 6. Common Workflows

#### Starting New Feature
1. CC detects main area, prompts for worktree
2. Create: `git worktree add _worktree/feature-name -b feature-name`
3. Work in `_worktree/feature-name/`
4. First push: `git push -u origin feature-name`
5. Regular commits and pushes

#### Switching Contexts
1. From any worktree: `cd ../../_worktree/other-feature/`
2. CC updates context automatically
3. Continue work in new context

#### Cleaning Up
1. After PR merge: `git worktree remove _worktree/feature-name`
2. Delete remote branch: `git push origin --delete feature-name`
3. Update main: `git pull origin main`

### 7. Emergency Procedures

#### Corrupted Worktree
```bash
# Remove and recreate
git worktree remove _worktree/{feature-name} --force
git worktree add _worktree/{feature-name} {feature-name}
```

#### Lost Worktree Reference
```bash
# Clean up phantom references
git worktree prune
```

## Claude Code Integration

### Required Prompts
- **Location Check**: "I notice you're in {location}. Would you like to work in a worktree instead?"
- **Worktree Selection**: "Available worktrees: {list}. Which would you like to use, or create new?"
- **Status Updates**: "Current worktree: {name} | Branch: {branch} | Status: {clean/dirty}"

### Automation Triggers
- Detect if in main area → prompt for worktree
- New work requested → suggest creating dedicated worktree
- File operations → ensure we're in appropriate worktree

### Safety Checks
- Warn before switching with uncommitted changes
- Confirm before removing worktrees
- Verify branch status before major operations

---

## Quick Reference

### Essential Commands
```bash
# Create worktree
git worktree add _worktree/{feature-name} -b {feature-name}

# List worktrees  
git worktree list

# Remove worktree
git worktree remove _worktree/{feature-name}

# Set up remote tracking
git push -u origin {feature-name}

# Navigate
cd _worktree/{feature-name}  # Enter worktree
cd ../../                    # Return to root
```

### CC Integration
- Always start at repo root
- Use `_worktree/` for all isolated work
- Let CC manage worktree lifecycle
- Focus on feature work, not git mechanics