# Git Workflow

## Branches

- `main`: stable version that should load in Space Engineers.
- `feature/<topic>`: new behavior, controls, visuals, settings, or gameplay ideas.
- `fix/<topic>`: narrow bug fixes.
- `refactor/<topic>`: internal cleanup with minimal behavior changes.
- `docs/<topic>`: documentation only.
- `release/<version>`: optional branch for Workshop packaging.

Examples:

```powershell
git switch main
git pull
git switch -c feature/multi-panel-output
```

See [ROADMAP.md](ROADMAP.md) for planned feature branches.

## Daily Loop

```powershell
git status
git pull
git switch -c feature/my-change

# edit and test in Space Engineers

git add .
git commit -m "Describe the change"
git push -u origin feature/my-change
```

After review/testing, merge the branch into `main`.

## Commit Style

Keep commits small enough that they describe one idea:

- `Add configurable scan interval`
- `Fix raycast hit filtering for subgrids`
- `Extract LCD drawing helpers`
- `Document panel CustomData settings`

## Before Merging

- Launch Space Engineers and confirm the mod loads.
- Check that a panel named `[GRID_SCHEMATICS]` updates.
- Test any changed `CustomData` setting.
- Keep `main` playable.
