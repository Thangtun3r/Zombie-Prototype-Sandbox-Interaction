# Design Direction

## Game concept

This is a first-person zombie shooter intended to eventually move the player along predetermined rails. The player does not freely navigate the environment. The mission-level goal is to reach the end of the route or safe point alive; killing every zombie is not necessarily required.

The rail system is not part of the current prototype. The player is deliberately represented by a stationary first-person camera while the zombie and combat foundations are established.

## Core identity

The player should manipulate the formation, position, flow, and timing of the zombie horde. Environmental objects are implementations of a coherent system, not unrelated combat gimmicks.

The intended mental loop is:

1. Observe the horde.
2. Understand the current situation.
3. Identify a problem or opportunity.
4. Choose an interaction and when to use it.
5. Observe how the formation changes.
6. Respond to the new situation.

The player should frequently ask, “I can do this now, but should I?”

## Current three-verb hypothesis

### MOVE

Change the horde's position or trajectory. Push and redirect are variations of MOVE.

Possible implementations include a fire extinguisher, fan, or water pressure. The meaningful question is where the player wants the horde to be, not merely how strong the push is.

### SLOW

Change the timing of the horde. SLOW should buy time rather than simply delete the threat.

Possible implementations include electricity or a temporary slowing zone. The meaningful question is whether to use the delay immediately or wait for a better timing window.

### BLOCK

Control where the horde can move. BLOCK can create accumulation, funnels, compression, splitting, temporary safety, and alternate routes.

Possible implementations include a crate, gate, or falling container. Timing should trade immediate safety against a potentially larger later payoff.

## Outcomes and payoffs are not automatically new verbs

- Push and redirect remain variations of MOVE.
- Compress, split, funnel, and scatter are formation outcomes.
- Explode and crush are payoffs or consequences.

Only add a new fundamental verb if playtesting reveals a recurring problem that MOVE, SLOW, and BLOCK cannot meaningfully express.

## Trade-offs

Powerful interactions should cost time, risk, distance, formation, position, safety, or a future opportunity. They do not all need arbitrary cooldowns or resource costs.

Examples:

- MOVE gains distance but may spread the horde.
- SLOW gains time but allows accumulation.
- BLOCK controls flow but may build a more dangerous mass behind it.
- An explosion used now gives immediate value but loses a better later grouping.

## Prototype purpose

The prototype is an experimental playground, not a full level editor. It should make it easy to place a horde, change formation, trigger interactions, control timing, reset, observe results, and compare action sequences.

The north star is:

> Few rules → many situations → meaningful choices.

Do not require elaborate combo chains. Situational sequences that respond to a changing horde are preferred.
