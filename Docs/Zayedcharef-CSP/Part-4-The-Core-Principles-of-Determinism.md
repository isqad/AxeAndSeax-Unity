Part 4: The Core Principles of Determinism - Zayed Charef - blog

# Part 4: The Core Principles of Determinism

Written by

[zayed.charef@gmail.com](https://zayedcharef.io/author/zayed-charefgmail-com/)

in

[Networking](https://zayedcharef.io/category/unity/networking/), [Unity](https://zayedcharef.io/category/unity/)

For Client-Side Prediction to work effectively, we have one primary goal:

Our gameplay code must strive to be 100% deterministic.

What does this mean? It means that for the same set of inputs, your code should produce the exact same outcome, every time, on any machine. If your client’s code calculates that`Input A` results in`State B`, the server’s code must do the same.

Now, you might be thinking,

But isn’t perfect determinism impossible in Unity because of things like floating-point math or physics?

And you’d be absolutely right. Tiny inaccuracies can and do occur between the client and server simulations.

This is where the magic of a good prediction system like PurrNet comes in. It’s designed to handle these minor discrepancies. When the server sends a corrected state, the client doesn’t just “snap” to the new position. It smoothly interpolates the correction over a fraction of a second, making tiny errors completely invisible to the player.

So, while the system can handle small mistakes, our job as developers is to not make its life harder. The “unbreakable rule” is really about our own code: we must avoid sources of major non-determinism. If our code is deterministic, the reconciliation system only has to correct tiny floating-point errors. If our code is not deterministic (e.g., using`UnityEngine.Random`), the errors will be large and frequent, leading to a jumpy, jittery experience for the player. This is a desync.

Let’s explore the common “determinism killers”—the major sources of non-determinism that we, as developers, must control.

---

## 1. The Randomness Problem

Computers can’t generate truly random numbers; they use complex algorithms seeded by a starting value. The problem is where that seed comes from.

### ❌ The Wrong Way: UnityEngine.Random

`UnityEngine.Random`(and`System.Random`) is often seeded by the system clock or other non-predictable values. This means`Random.Range(0, 10)` will produce a different sequence of numbers on your machine versus the server’s.

```
// ❌ NON-DETERMINISTIC: This will cause desyncs!
void Simulate(ref PlayerState state, float delta)
{
    // Every client and the server will get a DIFFERENT number.
    if (UnityEngine.Random.Range(0f, 1f) < 0.1f) 
        state.health += 10;
}

```

C#

### ✅ The Right Way: PredictedRandom

The core principle of deterministic randomness is that the Random Number Generator (RNG) itself must be part of the game state. It should be initialized once with a stable seed and then its state should be advanced with each use. Re-creating an RNG on-the-fly using volatile data like position is a recipe for desync.

PurrNet provides`PredictedRandom` for this exact purpose.

#### ✅ Step 1: Add the RNG to the State Struct

First, we add the`PredictedRandom` instance directly into the data structure that gets synchronized and reconciled by PurrNet. In our case, this is the AI’s state machine data.

```
using PurrNet.Prediction;

// ...

public struct FiniteStateMachineData : IPredictedData<FiniteStateMachineData>
{
    public int health;
    
    // ... other state variables ...

    // WHY: The RNG's state is now part of the data that PurrNet
    // will roll back and reconcile, ensuring its sequence of numbers
    // is always in sync between the client and the server.
    public PredictedRandom random;

    public void Dispose() { }
    
    // ... Equals() and GetHashCode() are updated to include 'random' ...
}
```

C#

#### ✅ Step 2: Initialize the RNG Once

First, we add the`PredictedRandom` instance directly into the data structure that gets synchronized and reconciled by PurrNet. In our case, this is the AI’s state machine data.

```
protected override AIState GetInitialState()
{
    // 1. Get the deterministic, networked ID from PurrNet.
    // This value is the same for this AI on all machines.
    uint deterministicSeed = (uint)id.objectId.instanceId.value;

    // 2. Ensure the seed is not 0 (a requirement for Unity.Mathematics.Random).
    if (deterministicSeed == 0)
    {
        deterministicSeed = 1;
    }

    // 3. Create the RNG and store it in the initial state.
    // This is the ONLY time we will call Create().
    return new AIState
    {
        FSM = new FiniteStateMachineData
        {
            random = PredictedRandom.Create(deterministicSeed)
        }
    };
}
```

C#

#### ✅ Step 3: Use and Advance the Stored RNG

Now, whenever any part of the AI’s logic needs a random number (like choosing a new patrol direction), it accesses the single, shared instance from the current state. Each call to`NextFloat()` not only returns a deterministic value but also advances the RNG’s internal state, ensuring the next random number will also be correct in the sequence.

```
private Vector2 GetNewDeterministicDirection()
{
    // 1. Get a reference to the single, stateful RNG instance.
    // We are not creating anything new here.
    ref var random = ref aiController.Module.FSM.currentState.random;

    // 2. Use it to get the next random value in its sequence.
    float angle = random.NextFloat(0f, 360f) * Mathf.Deg2Rad;
    return new Vector2(math.cos(angle), math.sin(angle));
}
```

C#

This three-step pattern ensures that our AI’s “random” decisions are perfectly repeatable and synchronized across the network, completely eliminating this category of desync bugs.

---

## 2. The Time Problem

Time seems simple, but in game engines, it’s tied to how fast your computer can render frames.

### ❌ The Wrong Way: Time.deltaTime

`Time.deltaTime` is the time elapsed since the last frame. If you are running at 144 FPS,`Time.deltaTime` will be small (0.0069s). If your friend is running at 60 FPS, their`Time.deltaTime` will be larger (0.0166s).

If you calculate movement using this, the player running at 144 FPS will move a shorter distance per frame, but more frequently, while the 60 FPS player will move a larger distance less frequently. Over many frames, rounding errors in floating-point math will accumulate, causing a desync.

```
// ❌ NON-DETERMINISTIC: Player movement will differ based on FPS.
void Simulate(ref PlayerState state, float delta)
{
    // This is a desync waiting to happen!
    state.position += state.velocity * Time.deltaTime; 
}

```

C#

### ✅ The Right Way: The Fixed delta

PurrNet operates on a fixed tick rate (e.g., 30 ticks per second). This means the simulation always advances in fixed time steps, completely independent of the framerate. In my project, this is`1/30 = 0.0333...` seconds per tick.

PurrNet provides this fixed time step as the`delta` parameter in its`Simulate` methods. You must always use this`delta`.

#### ✅ A Good Example from Our Project:

`PlayerMovementModule.cs` correctly uses the`delta` provided by PurrNet to calculate the new velocity, ensuring the physics calculations are identical regardless of framerate.

```
private void ApplyMovementPhysics(Vector2 inputVector, float speedMultiplier, float lerpSpeed, float delta)
{
    var currentVelocity = playerController.PredictedRb.linearVelocity;
    var newVelocity = MovementUtility.CalculateMovementVelocity(
        currentVelocity,
        inputVector,
        data.maxSpeed,
        speedMultiplier,
        lerpSpeed,
        delta // ✅ USING THE DETERMINISTIC, FIXED DELTA
    );

    playerController.PredictedRb.linearVelocity = newVelocity;
}

```

C#

---

## 3. The Collection Order Problem

When you ask Unity for a list of objects, like`FindObjectsByType`, there is no guarantee about the order in which you’ll get them. If the server gets`[PlayerA, PlayerB]` and a client gets`[PlayerB, PlayerA]`, and your code processes them in that order, the simulation will diverge.

### ❌ The Wrong Way: Unordered Collections

```
// ❌ NON-DETERMINISTIC: The order of this array can be different for everyone.
var scenePlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
foreach (var player in scenePlayers)
{
    // Processing players in a random order will lead to desyncs if they interact.
}

```

C#

### ✅ The Right Way: Stable Sorting

You must always enforce a stable, deterministic sort order on any collection you iterate over. The best way is to sort by a value that is unique and consistent for each object, like a`NetworkID`.

#### ✅ A Critical Fix from my Project:

`PlayerManager.cs` correctly finds all`PlayerController` objects and then immediately sorts them by their`id`‘s hash code before processing them. This ensures the list of players is in the same order on the server and all clients.

```
// Player.PlayerManager.cs

private void DiscoverScenePlayers(ref PlayerManagerState state)
{
    // 1. Get the players in whatever order Unity provides.
    var scenePlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None)
        // 2. ✅ CRITICAL: Immediately sort them into a predictable order.
        .OrderBy(p => p.id.GetHashCode()) 
        .ToArray();

    state.activePlayerIds.Clear();

    // 3. Now, process them in a guaranteed-deterministic order.
    foreach (var player in scenePlayers) 
    {
        state.activePlayerIds.Add(player.id);
    }
}

```

C#

---

### Next Up: Part 3 – Reconciliation & State Management

Now that we understand how to keep our simulation deterministic, we’ll explore what happens when it breaks, and how the magic of reconciliation fixes it.

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
