Part 3: PredictedIdentity in Practice – The Core Methods - Zayed Charef - blog

# Part 3: PredictedIdentity in Practice – The Core Methods

Written by

[zayed.charef@gmail.com](https://zayedcharef.io/author/zayed-charefgmail-com/)

in

[Networking](https://zayedcharef.io/category/unity/networking/), [Unity](https://zayedcharef.io/category/unity/)

Now that we understand the high-level workflow, let’s get practical. When you create a script that inherits from`PredictedIdentity`, you’ll be working with a handful of key`override` methods. Understanding what each one does—and more importantly, what it shouldn’t do—is the key to writing clean and effective networking code.

---

## The Input Lifecycle: UpdateInput & GetFinalInput

As we covered in Part 2, handling input correctly is vital. PurrNet splits this into two phases:

`protected override void UpdateInput(ref INPUT input)`

- ✅ What it’s for: Accumulating single-action inputs (like Dash or Jump) that might happen between simulation ticks. Use`|=` to make sure you don’t miss a button press.
- When it runs: Every single visual frame (`Update`).

`protected override void GetFinalInput(ref INPUT input)`

- ✅ What it’s for: Setting continuous inputs (like movement axes) to their final value for this tick. This is also when your accumulated single-action inputs are “consumed” by the simulation.
- When it runs: Just before the simulation tick (`Simulate()`).

```
// Player._Module.Input.PlayerInputModule.cs

// Runs every frame to catch clicks
protected override void UpdateInput(ref PlayerInputData input)
{
    input.dashInput |= _playerLocalInput.IsDashInputPressed();
}

// Runs once per tick to set the final state
protected override void GetFinalInput(ref PlayerInputData input)
{
    input.horizontalInput = _playerLocalInput.MovementInputQueued.x;
}

```

C#

---

## The Core Loop: Simulate() vs. UpdateView()

This is the most important architectural separation in the entire system.

### protected override void Simulate(ref STATE state, float delta)

- 🚨 The Rule: The code in here must be deterministic. It should only modify the state based on inputs and timers. NEVER put visual effects, sound, or animations in here (with some specific exceptions, which we’ll cover).
- Key Parameter: It gives you`ref STATE state`, which is a direct reference to the`currentState`. This is the live, predicted state that you will modify.
- When it runs: Once per simulation tick (30 times per second for us). It also runs at high speed during a rollback/replay.
- What it’s for: GAMEPLAY LOGIC ONLY ⚠️

```
protected override void Simulate(ref PlayerMovementStateData state, float delta)
{
    // Good: Reading input, calculating physics, changing state variables.
    var inputVector = ProcessMovementInput();
    var newVelocity = MovementUtility.CalculateMovementVelocity(..., delta);
    playerController.PredictedRb.linearVelocity = newVelocity;

    // BAD: Do NOT do this here!
    // Instantiate(myParticleEffect);
    // myAudioSource.PlayOneShot(sound);
}

```

C#

### protected override void UpdateView(STATE interpolatedState, STATE? verified)

Key Parameters:

- `STATE? verified`: The last known state that the server has confirmed as being 100% correct.`verified.HasValue` will be`true` once the server has sent at least one update. You can use this to decide if you should play a critical animation.
- `STATE interpolatedState`: A smoothed-out, “in-between” version of your state. Use this for things like UI or effects, to ensure it moves smoothly instead of stuttering from tick to tick.

```
protected override void UpdateView(PlayerStateData interpolatedState, PlayerStateData? verified)
{
    // Good: Playing animations that are safe to predict for our own character.
    if (AnimationSystemUtility.ShouldUpdateAnimationsInView(predictionManager, this))
    {
        playerController.AnimationSystem?.UpdateAnimations(
            playerController.PredictedSm.currentStateNode,
            verified.HasValue // We can pass this down to the animation system
        );
    }
}

```

C#

---

## Handling Other Players: ModifyExtrapolatedInput

When you don’t have fresh input data for a non-owned object (e.g., due to packet loss), the system will “extrapolate” by re-using the last known input. This can cause a remote player to keep walking forward and then snap back when the correct data arrives.

`protected override void ModifyExtrapolatedInput(ref INPUT input)`

- What it’s for: To gracefully degrade the extrapolated input. For example, you can reduce the movement input so the remote character smoothly slows to a stop instead of walking forever. This makes minor packet loss much less noticeable.
- When it runs: During the simulation of a non-owned object when the system is missing new input and has to guess.

```
// Player._Module.Input.PlayerInputModule.cs

// This makes remote players feel much smoother during minor network issues.
protected override void ModifyExtrapolatedInput(ref PlayerInputData input)
{
    // Gradually reduce movement input to zero.
    input.horizontalInput *= 0.6f;
    input.verticalInput *= 0.6f;

    // Snap to zero when it's very small to ensure a complete stop.
    if (Mathf.Abs(input.horizontalInput) < 0.2f) input.horizontalInput = 0f;
    if (Mathf.Abs(input.verticalInput) < 0.2f) input.verticalInput = 0f;
}

```

C#

We will cover extrapolation and interpolation in more detail in a future guide. For now, just know that this method is a powerful tool for improving the visual quality of remote entities.

---

### Next Up: Part 4 – The Core Principles of Determinism

Now that we are familiar with the main methods, let’s dive into the strict rules we must follow inside`Simulate()` to ensure our predictions are accurate.

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
