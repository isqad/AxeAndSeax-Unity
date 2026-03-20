Part 1: The “What & Why” of Client-Side Prediction - Zayed Charef - blog

# Part 1: The “What & Why” of Client-Side Prediction

Written by

[zayed.charef@gmail.com](https://zayedcharef.io/author/zayed-charefgmail-com/)

in

[Networking](https://zayedcharef.io/category/unity/networking/), [Unity](https://zayedcharef.io/category/unity/)

Welcome to the world of real-time multiplayer networking! This guide will walk you through the core concepts of Client-Side Prediction (CSP) as implemented in PurrNet/PurrDiction, using examples from my own project.

## The #1 Enemy in Online Games: Latency

Ever pressed a button in a game and felt that annoying delay before your character reacts? That’s latency (or “lag”). It’s the time it takes for your input to travel to the game server and for the server’s response to travel back to you.

In a simple server-authoritative model, the flow is:

1. Your game receives the new position and finally updates your character on screen.
2. The server sends the new position back to you and all other players.
3. The server receives it, moves your character, and calculates its new position.
4. The “move forward” command is sent to the server.
5. You press the “move forward” button.

With a latency of 100ms, your character won’t move for 0.1 seconds after you press the button. It feels sluggish and unresponsive.

## The Solution: Predict the Future!

Client-Side Prediction solves this by making a simple but powerful assumption: “The server will probably agree with my action.”

With CSP, the flow becomes:

1. The server sends the authoritative state back to you. (We’ll cover what happens if the prediction was wrong in the “Reconciliation” section).
2. The server runs its own simulation and calculates the “true” outcome.
3. Simultaneously, the “move forward” input is sent to the server.
4. Instantly, your local game predicts the outcome and moves your character forward on your screen. It feels immediate and responsive.
5. You press “move forward”.

## Server Authority: The Unquestionable Source of Truth

While we predict locally, we must always respect the server. The server is the law. This is the essence of a Server-Authoritative Model.

This architecture is the key to preventing cheating. A client can’t just tell the server, “My health is now 1,000,000” or “My position is inside the enemy’s vault.” The client can only say, “I pressed the ‘move forward’ key” or “I pressed the ‘fire’ button.”

The server receives these inputs, runs its own simulation, and determines the true outcome. If a player is trying to move through a wall, the server’s simulation will simply stop them.

### Why This Prevents Cheating

Because the only thing we “trust” from a client is their inputs. The entire game state (position, health, ammo, etc.) is calculated and validated by the server.

This leads to our first critical rule…

## Input Sanitization: Don’t Trust Blindly

Even though we only trust inputs, a malicious client could still try to abuse them. What if a player sends 1,000 “move forward” inputs in a single second? Without protection, their character would fly across the map, effectively speed-hacking.

This is why Input Sanitization is crucial. We must validate and clamp inputs to reasonable values.

#### ✅ A Good Example from my Project:

In`PlayerMovementModule.cs`, we ensure the movement input vector can’t be larger than 1. This prevents a player from sending a modified input like`(x: 5, y: 5)` to move five times faster.

```
private Vector2 ProcessMovementInput()
{
    var inputVector = new Vector2(playerController.CurrentInput.horizontalInput,
        playerController.CurrentInput.verticalInput);

    // ✅ CRITICAL: Clamp the magnitude to 1.
    // This ensures the player cannot move faster than intended by sending oversized inputs.
    if (inputVector.magnitude > 1f)
        inputVector = inputVector.normalized;

    return inputVector;
}
```

C#

This simple check, performed on both the client (for immediate feedback) and the server (for security), is a fundamental part of a secure CSP architecture.

---

### Next Up: Part 2 – The PurrNet Workflow – From Input to Simulation

In the next section, we’ll explore the general PurrNet workflow with client-side prediction.

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
