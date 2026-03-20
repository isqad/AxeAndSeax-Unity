Part 7: Ownership & Animation – The Definitive Guide - Zayed Charef - blog

# Part 7: Ownership & Animation – The Definitive Guide

Written by

[zayed.charef@gmail.com](https://zayedcharef.io/author/zayed-charefgmail-com/)

in

[Networking](https://zayedcharef.io/category/unity/networking/), [Unity](https://zayedcharef.io/category/unity/)

Animations are tricky in a Client-Side Prediction (CSP) model. Do you play them instantly and risk them being wrong? Or do you wait for the server and risk them feeling laggy? The answer depends on ownership and the gameplay context.

This guide explains the advanced logic used in our project to handle every animation case correctly.

---

## The 4 Horsemen of CSP Context: isOwner, isServer, isVerified, isVerifiedAndReplaying

To make smart decisions about animation, you first need to understand the context you’re in. PurrNet provides several boolean properties that tell you exactly what’s happening.

`isOwner`:

- Server:`true` for all AI (since the server controls them).`false` for all player-controlled characters (even though it has authority, it doesn’t “own” their inputs).
- Client:`true` for your own player character.`false` for everyone else’s character and all AI.
- What it means: “Is this game instance the one that directly controls this object?”

`isServer`:

- Server/Host: Always`true`.
- Client: Always`false`.
- What it means: “Is this code running on the server/host?”

`isVerified`:

- Server/Host: Always`true` during its normal simulation, because the server is the source of truth.
- Client: Only`true` during a rollback and re-simulation (a.k.a. “replaying”). It’s`false` during normal, live prediction.
- What it means: “Is the simulation tick we are currently processing based on data that has been confirmed by the server?”

`isVerifiedAndReplaying`:

- Server/Host:`false` during normal simulation.
- Client:`true` only during a replay. This is your go-to flag for triggering “correctness-first” logic.
- What it means: This is a convenient property that essentially combines`isVerified && isReplaying`. It’s the most reliable way for a client to know: “Am I currently in a re-simulation process based on a server-corrected state?”

---

## The Great Divide: UpdateView() vs. Simulate()

As mentioned in the previous guides, our project uses an architectural split to manage animations based on our needs.

### ShouldUpdateAnimationsInView() → For Owned Objects

- Why: This is for maximum responsiveness. When you press a button, you want to see your character animate instantly. Since you are the source of the inputs, your predictions are highly likely to be correct. We run this in`UpdateView()` which uses the smooth, interpolated state, making your character’s movement feel fluid. The system architecture guarantees that`UpdateView()` is never called during a rollback, so we only need to check for ownership.
- When it’s true: When the object is yours (`isOwner` is true).

### ShouldUpdateAnimationsInSimulate() → For Non-Owned Objects

- Why: This is for maximum correctness. You have no idea what another player or an AI is going to do. Their state is constantly being predicted (extrapolated) and corrected. If you animated them based on your potentially wrong prediction, you’d see them stutter and pop constantly. By waiting for a`isVerifiedAndReplaying` tick, you guarantee the animation you are about to play corresponds to what the server actually said happened.
- When it’s true: When the object is not yours AND the server is running the code OR you are a client replaying a verified tick.

---

## The Trade-Off: Responsiveness vs. Correctness

You can’t always have both. Sometimes you must choose.

### The Problem with Animating Non-Owned Objects in UpdateView()

`UpdateView()` runs every visual frame and uses an interpolated state. This state is a smooth “guess” between two past authoritative states sent by the server. If a client’s prediction about that object was wrong, the interpolated state is also a lie.

Scenario:

1. Result: The player sees the AI animate left for a few frames before instantly appearing on the right and running right. It looks like a bug.
2. Reconciliation happens. The AI model snaps to its correct position.
3. The server’s state arrives and says, “Actually, the AI moved right.”
4. The`UpdateView()` interpolates this predicted movement and plays the “Run Left” animation.
5. Your client predicts an enemy AI will move left.

By animating non-owned objects only on verified ticks inside`Simulate()`, we avoid this visual lie. We accept a tiny delay (~50-100ms, the player’s latency) in exchange for the animation always matching the character’s true actions.

### When to Break the Rules: The Case for Predicted Effects

Sometimes, instant feedback is more important than 100% accuracy. This is a deliberate design choice.

Death Animations: This is the opposite. A death animation is a final, critical state change. It would be incredibly weird and frustrating for a player to see an enemy start dying, only for it to snap back to life because the prediction was wrong (e.g., the server determined your shot missed).

- Therefore, in our project, the`PlayerDead` state change and its animation MUST wait for server verification. We sacrifice a little responsiveness for absolute correctness on critical gameplay events.

---

## 📋 Animation Checklist

- Is this my character? (`isOwner`)

[✅] Animate in`UpdateView()` for maximum responsiveness.

- Is this another player’s character or an AI? (`!isOwner`)

[✅] Animate in`Simulate()` but guard it with`isVerifiedAndReplaying`(or`isServer`) for correctness.

- Is this a low-impact, feedback-oriented animation (like a hit flash)?

[✅] Consider playing it predictively in`UpdateView` for all players to make the game feel more impactful. Accept the risk of it being rolled back.

- Is this a critical, gameplay-defining animation (like death, a stun, or a powerful ultimate ability)?

[✅] Never play this predictively for non-owned objects. Always wait for the server’s authority.

## Comments

[September 22, 2025](https://zayedcharef.io/part-7-ownership-animation-the-definitive-guide/#comment-4)

[Tad](https://taddidio.com/)

Great series, thanks for sharing your ideas!

One question I have about your animation process is whether you use the Unity animator or not. It seems to me like you either need to have an edge detector (for state changes) that works in UpdateView() so that your animator can react properly or you need to call the animator.Play(clip, layer, t); functions each frame and discard the animator graph transitions.

The animator with edge detectors would be much preferred because it works with Unity instead of against it but I’m not sure if it’s possible since you will keep reconciling and restarting until the confirmed server action gets back, effectively losing the benefit of prediction.

Thanks for all your insight

Reply

[October 3, 2025](https://zayedcharef.io/part-7-ownership-animation-the-definitive-guide/#comment-6)

[zayed.charef@gmail.com](http://zayedcharef.io/)

Hello Tad 🙂 Yes I use the Unity Animator but only to reference my animations. The Animator is then controlled manually by the code, using Animator.Play(…). This allows me to have a granular control over animations and only play animations on certain network conditions, to avoid animations being reconciled etc. It is maybe possible with an edge detector but I find my choice the most simple and logical to me.

Reply

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
