Part 2: The PurrNet Workflow – From Input to Simulation - Zayed Charef - blog

# Part 2: The PurrNet Workflow – From Input to Simulation

Written by

[zayed.charef@gmail.com](https://zayedcharef.io/author/zayed-charefgmail-com/)

in

[Networking](https://zayedcharef.io/category/unity/networking/), [Unity](https://zayedcharef.io/category/unity/)

In Part 1, we covered why we use Client-Side Prediction. Now, let’s look at how PurrNet’s “PurrDiction” system makes it happen. This page will give you the high-level map of the key components and the lifecycle of a single predicted tick, providing the context for the`Simulate()` methods we’ll discuss later.

---

## The Core Components: A 2-Part System

At its heart, the PurrDiction system has two main parts that work together:

1. `PredictedIdentity`: These are the “Actors” in our world. Any object that needs to be predicted (players, AI, projectiles) must have a script that inherits from`PredictedIdentity`. This is the component where we will write all of our gameplay logic. [[Source: PurrNet Docs – Predicted Identities](https://purrnet.gitbook.io/docs/tools/client-side-prediction/predicted-identities)]
2. `PredictionManager`: Think of this as the “World” or the “Director”. There is one`PredictionManager` per scene. Its job is to orchestrate the entire simulation. It manages a list of all predicted objects and is responsible for advancing the simulation tick by tick, handling rollbacks, and triggering reconciliations. You will rarely interact with it directly, but it’s the engine running everything under the hood. [[Source: PurrNet Docs – Overview](https://purrnet.gitbook.io/docs/tools/client-side-prediction/overview)]

---

## Choosing Your “Identity”: The 3 Flavors of Prediction

PurrNet gives us three base classes to inherit from, depending on what our object needs to do. Choosing the right one is the first step in creating a networked object.

### 1. PredictedIdentity (For Player-Controlled Objects)

- Example: While our project uses a modular approach, a simple Player Controller would look like this
- Use Case: Perfect for a player character that directly responds to key presses.
- What it is: An identity that takes player`INPUT` to modify its`STATE`.

```
public struct PlayerInput { public Vector2 Move; public bool Jump; }
public struct PlayerState { public Vector3 Position; public Vector3 Velocity; }

public class SimplePlayer : PredictedIdentity<PlayerInput, PlayerState>
{
    protected override PlayerInput GetInput() { /* Code to read keyboard/gamepad */ }
    protected override void Simulate(PlayerInput input, ref PlayerState state, float delta) { /* Move player based on input */ }
}
```

C#

### 2. PredictedIdentity (For State-Driven Objects)

- A Perfect Example from Our Project:
- Use Case: Perfect for AI, physics-based projectiles, or moving platforms.
- What it is: An identity that has a`STATE` but is not directly controlled by player input. Its state changes based on time, physics, or internal logic.

```
// AI._Module.Movement.AIMovementModule.cs
public class AIMovementModule : PredictedIdentity<AIMovementStateData>
{
    // This AI's movement isn't driven by player input,
    // but its state (position, velocity) still needs to be predicted and reconciled.
    protected override void Simulate(ref AIMovementStateData state, float delta)
    {
        // ... AI pathfinding logic goes here ...
    }
}
```

C#

### 3. StatelessPredictedIdentity (For Logic Handlers)

- A Perfect Example from Our Project:
- Use Case: Ideal for manager classes or controllers that coordinate other`PredictedIdentity` components.
- What it is: A special type of identity that has no state of its own to reconcile. It simply hooks into the`Simulate()` loop to run logic every tick.

```
// Player.PlayerController.cs
public class PlayerController : StatelessPredictedIdentity
{
    // This controller doesn't have its own state. Instead, it reads input
    // and tells its various modules (Movement, Combat, etc.) how to behave.
    // It's the "brain" coordinating the stateful "limbs".
}
```

C#

---

## The Lifecycle of a Predicted Tick

While we will dive into the specific`override` methods in the next section, it’s helpful to understand the high-level lifecycle that happens for every predicted object on every tick:

1. Reconcile (If Necessary): If a state update arrives from the server that contradicts the client’s saved history, the`PredictionManager` triggers a rollback. It effectively says, “Wait, my history at tick 105 was wrong.” It then rewinds to the last correct state and rapidly re-runs the simulation for every tick from that point to the present, using the saved inputs to produce a new, corrected history.
2. Save State: The new state produced by the simulation is saved into a history buffer, timestamped with the current tick. This history is essential for the magic of reconciliation.
3. Simulate State: The`Simulate()` method is called. This is where all your deterministic gameplay logic lives—updating position, changing health, modifying state based on the input from step 1.
4. Gather Input: For player-controlled objects, PurrNet uses a sophisticated system to gather input. It ensures that quick, single-frame actions (like a dash command) aren’t missed between simulation ticks, while continuous actions (like holding a move key) are read at the last possible moment for accuracy. This process is explained in detail in the next part.

---

## For More Detail…

This guide covers the core workflow and best practices we use in our project. However, PurrNet is a deep and powerful library. To understand exactly how components like the`PredictionManager` or the`PredictedHierarchy` work under the hood, we strongly encourage you to read the [official PurrNet documentation](https://purrnet.gitbook.io/docs/tools/client-side-prediction/overview) in detail.

---

### Next Up: Part 3: PredictedIdentity in Practice – The Core Methods

Now that we know the workflow and how to handle input, we’ll dive deep into the rules we must follow inside`Simulate()` to ensure our predictions match the server’s calculations.o ensure our predictions match the server’s calculations.

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
