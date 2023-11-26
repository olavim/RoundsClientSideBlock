# Client side block

Makes blocking more consistent in online play by syncing block messages more aggressively.

## Strategies

When a player blocks, they notify other players immediately. However, there is some delay until the message gets to its destinations, resulting in a window where a player who blocked can still get hit by a bullet if its shooter didn't receive the message soon enough. This mod offers two solutions to the problem.

The strategy can be chosen from the mod's option menu.

### Optimistic syncing (default)

Since we can assume that information about each block will arrive as soon as possible, then theoretically we just need to wait until it does before we decide whether to apply damage or to reflect a bullet. Problem is, we don't know _when_ the information is coming exactly, and we can't wait forever.

Information about each player's ping is recorded periodically. This information can be used to estimate how long a target's block message would take, on average, to reach the shooter. The optimistic syncing strategy waits for this estimate (plus some extra wiggle room) to pass before determining whether the target blocked or not.

When players have a good, stable internet connection, this strategy works just fine. However, if players have a highly fluctuating ping which results in the strategy's estimate to be too far off, the strategy's performance will degrade and you might want to use the second strategy. However, using this strategy should result in more consistent blocking compared to the base game no matter what.

### Pessimistic syncing

With this strategy, when a bullet hits a player (from the shooter's perspective), the shooter asks the target whether they blocked the bullet or not, and waits for their answer. This works even with unreliable internet connections, but there is a bigger delay before a bullet deals damage or is reflected.
