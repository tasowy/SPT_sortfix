# AutoSortFix (0.1.1)

Workaround for the vanilla stash auto-sort FPS bug: sorting fires a per-item
`GridView` add/remove event burst that orphans ~40x live `GridItemView`s,
permanently halving FPS until the stash screen is closed.

The mod suppresses per-item grid-view handling while a sort runs, then rebuilds
each grid once from the model (`Grid.ContainedItems`). Model moves are untouched.

Toggle in F12 menu: `SPT_sortfix` -> `Enabled` (off = 100% vanilla behavior).
