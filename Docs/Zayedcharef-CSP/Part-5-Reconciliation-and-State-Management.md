Part 5: Reconciliation & State Management - Zayed Charef - blog

# Part 5: Reconciliation & State Management

Written by

[zayed.charef@gmail.com](https://zayedcharef.io/author/zayed-charefgmail-com/)

in

[Networking](https://zayedcharef.io/category/unity/networking/), [Unity](https://zayedcharef.io/category/unity/)

In the previous guides, we established that reconciliation is the core process for synchronizing the client with the server’s authoritative state. Now, let’s dive deeper into the mechanics of how this “rewind and replay” magic actually works and what it demands from our code.

## The Reconciliation Process: A Deeper Look

While the concept is simple (server corrects client), the process is a precise, high-speed sequence of events. Let’s walk through the client’s internal monologue during a typical reconciliation:

1. “Fast-forward to the present (Re-Simulate)”: I now Re-Simulate, instantly running my`Simulate()` logic for every tick from`105` up to my current local tick (e.g.,`108`), using my saved inputs. This generates a new, corrected prediction timeline.
2. “Time to rewind (Rollback)”: I perform a Rollback. I throw away my incorrect predictions from tick`105` onwards and reset my current state to exactly what the server told me for tick`105`.
3. “Let me check my notes”: I look at my own predicted history for tick`105`. The server’s update says my position is`(10, 5)`, but my prediction was`(12, 5)`. My prediction was wrong! This could be due to my own latency or, more commonly, because another player’s action (which I just learned about) affected me.
4. “Truth has arrived”: A packet comes in from the server. It contains state data for tick`105`.

Because this all happens in a single frame, the player just sees their character smoothly correct its course.

This entire process relies on one crucial thing: a well-designed State Struct.

## State Management: What to Reconcile?

The state struct (the one that implements`IPredictedData`) is the “save file” for a given tick. It must contain the absolute minimum data required to perfectly re-simulate the future.

Putting too much data in the state wastes network bandwidth and CPU. Putting too little data in makes deterministic replay impossible.

### ✅ DO: Include in the State

Think of these as the “seeds” of your gameplay logic.

- Critical Timers & Counters: Anything that controls when an action happens

```
public struct MyAIState : IPredictedData<MyAIState>
{
  public float timeTillNextPatrolAction; // Determines when the AI changes its patrol behavior.
  public float attackTimer;              // Controls cooldowns or charge-up times for attacks.
}
```

C#

- Logical State: Booleans or enums that fundamentally change an object’s behavior.

```
public struct MyAIState : IPredictedData<MyAIState>
{
  public uint health; // My AI's health
  public PatrolPhase patrolPhase; // Is the AI moving or pausing? Essential for replay.
}
```

C#

### ❌ DON’T: Exclude from the State

- Static Configuration Data: Values that don’t change during gameplay. These should be in a`ScriptableObject`.

```
// ❌ WRONG: This data is static. Don't put it in the state.
public struct BadAIState : IPredictedData<BadAIState>
{
    public float maxSpeed;      // This is in AIDataSO, it never changes, no need to reconcile that.
    public float attackDamage;  // This is in AIDataSO, it never changes.
}
```

C#

- Derived Data: Values that can be calculated from other state variables. This is important to keep good performance.

```
// ❌ WRONG: Speed and isMoving can be derived from velocity.
public struct BadPlayerState : IPredictedData<BadPlayerState>
{
    public Vector2 velocity; // ✅ GOOD: The source of truth. (You don't need to add the velocity in the state if you're using a PredictedRigidbody, PurrNet handles that for you)
    public float speed;      // ❌ BAD: Calculate this with velocity.magnitude when needed.
    public bool isMoving;    // ❌ BAD: Calculate this with velocity.magnitude > 0.1f when needed.
}
```

C#

- References to Unity Components: You cannot serialize a`Transform` or`Rigidbody2D`. The state should be pure data.

```
// ❌ WRONG: These are Unity objects, not data.
public struct BadState : IPredictedData<BadState>
{
    public Transform target;        // Cannot be reconciled.
    public Rigidbody2D rb;          // Cannot be reconciled.
}
```

C#

Ask yourself this question for every variable:

“If I delete this variable from the state, is it IMPOSSIBLE for me to perfectly re-simulate the entity’s behavior from a rollback?”

If the answer is YES, it belongs in the state. If NO, it probably doesn’t.

---

## 🔧 ref var state = ref currentState – Why It’s CRITICAL

In C#,`struct` s are value types. When you assign them, you create a full copy.

- ✅ With`ref`:`ref var state = ref currentState;` creates a direct reference. Changes to`state` instantly affect`currentState` with zero copying. It’s faster, safer, and cleaner.
- ❌ Without`ref`:`var state = currentState;` makes a copy. Any changes to`state` are lost unless you copy it back with`currentState = state;`. This is slow and bug-prone.

Use`ref` for writing to the state. Use direct access (`currentState.myVar`) for reading.

### Real Performance Difference Example

```
// Complex state structure
public struct ComplexState : IPredictedData<ComplexState>
{
    public Vector3 position; // ❌ Remember, no need to add that to the state if you're using PurrNet's PredictedRigidbody
    public Vector3 velocity; // ❌ No need
    public Quaternion rotation; // ❌ No need
    public float health;
    public float energy;
    public bool[] abilities; // 50 booleans
    public PredictedRandom random;
    // Total: ~300 bytes
}

// ❌ SLOW - Copies 300 bytes every change
public void UpdateHealth(float damage)
{
    var state = currentState; // Copies 300 bytes
    state.health -= damage;
    currentState = state;     // Re-copies 300 bytes
}

// ✅ FAST - Modifies 4 bytes (float) directly
public void UpdateHealth(float damage)
{
    ref var state = ref currentState; // No copy
    state.health -= damage;
}
```

C#

---

## 📋 “Reconciliable State” Checklist

### ✅ INCLUDE in state:

- IDs of targets or key references
- Deterministic RNG (`PredictedRandom`)
- Logical states (phases, modes)
- Timers and gameplay counters
- Position, rotation, velocity (PurrNet already handles that if you’re using a`PredictedRigidbody`

### ❌ EXCLUDE from state:

- Debug or UI data
- Caches or optimizations
- Unity object references (Transform, Rigidbody)
- Constants and config (e.g. maxSpeed)
- Computable values (e.g. speed from velocity)

## 🎯 To sum up, the Golden Rule for Deciding

### Simple Test: “The Time Travel Test”

Imagine explaining what happened in the game at tick 100, and someone needs to reproduce ticks 101, 102, 103…

Ask yourself:

1. “Does this data change during the simulation?” → NO = Exclude it
2. “Can this data be recalculated from other data?” → YES = Exclude it
3. “Without this data, will the simulation be different?” → YES = Include it

## Comments

### Leave a Reply Cancel reply

Your email address will not be published. Required fields are marked *

Comment *

Name *

Email *

Website

Save my name, email, and website in this browser for the next time I comment.

Δ

## More posts

### Part 7: Ownership & Animation – The Definitive Guide

[August 8, 2025](https://zayedcharef.io/part-7-ownership-animation-the-definitive-guide/)

### Part 6: Performance — The Basics to Get You Going

[August 8, 2025](https://zayedcharef.io/part-6-performance-the-basics-to-get-you-going/)

### Part 5: Reconciliation & State Management

[August 7, 2025](https://zayedcharef.io/part-5-reconciliation-state-management/)

### Part 3: PredictedIdentity in Practice – The Core Methods

[August 7, 2025](https://zayedcharef.io/part-3-predictedidentity-in-practice-the-core-methods/)
