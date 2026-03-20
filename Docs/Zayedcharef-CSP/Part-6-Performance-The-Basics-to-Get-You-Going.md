Part 6: Performance — The Basics to Get You Going - Zayed Charef - blog

# Part 6: Performance — The Basics to Get You Going

Written by

[zayed.charef@gmail.com](https://zayedcharef.io/author/zayed-charefgmail-com/)

in

[Networking](https://zayedcharef.io/category/unity/networking/), [Unity](https://zayedcharef.io/category/unity/)

Now that you understand determinism and state, let’s focus on two areas that will make or break your game’s quality: performance and architecture. A fast, well-structured codebase is easier to debug and more enjoyable to play.

## Performance Killers in Simulate()

The`Simulate()` method is the heart of your game. It runs every single tick (30 times per second in our case) for every predicted object. If it’s slow, your entire game will suffer. Here are the two most common performance killers.

### 1. Memory Allocation & The Garbage Collector (GC)

What is Garbage Collection? Think of memory as a big workspace. When you create a new object (like with`new List ()`), you’re putting a new item on a workbench. The Garbage Collector is a janitor that periodically has to stop everything, find all the items that are no longer being used, and sweep them away to free up space.

This “stop and sweep” process causes a stutter or hitch in your game. In a simulation that needs to be smooth, these hitches are unacceptable. The key to performance is to create as little “garbage” as possible.

#### ❌ The Wrong Way: new List ()

Using`new List ()` inside a simulation loop is one of the worst offenders. You’re creating a new list, using it for a fraction of a second, and then throwing it away, forcing the GC to clean it up later.

```
// ❌ BAD: Creates garbage every time it's called.
public void FindNearbyEnemies()
{
    var nearby = new List<AIController>(); // Creates a new list (garbage!)
    // ... finds enemies and adds to list
} // The list is now garbage, waiting to be collected.

```

C#

#### ✅ The Right Way: Pooling with DisposableList 

PurrNet provides a powerful solution: Object Pooling. Instead of creating and destroying lists, we “rent” them from a pre-made pool and “return” them when we’re done.`DisposableList ` is your best friend for this.

It’s designed to be used with a`using` block, which automatically returns the list to the pool when you’re done. No garbage is created!

##### ✅ A Perfect Example from Our Project:

`PlayerManager.cs` needs a list to store player IDs. Instead of creating a`new List`, it correctly uses`DisposableList.Create()`. This list is part of the state and is managed by PurrNet’s pooling system.

```
// Player.PlayerManager.cs

protected override PlayerManagerState GetInitialState()
{
    return new PlayerManagerState
    {
        // ✅ PERFECT: Rents a list from the pool instead of creating new garbage.
        activePlayerIds = DisposableList<PredictedComponentID>.Create(4) 
    };
}

// And it's properly disposed of when the state is no longer needed.
public void Dispose()
{
    activePlayerIds.Dispose();
}

```

C#

### 2. Expensive Function Calls: GetComponent()

Well this is more a Unity specific related stuff but as you know, calling expensive methods such as`GetComponent ()` is slow. It has to search through every component on a GameObject to find the one you’re looking for. Doing this every tick for dozens of objects will quickly slow down your game.

#### ❌ The Wrong Way: GetComponent in the loop

```
// ❌ BAD: This searches for the Rigidbody 30 times per second!
protected override void Simulate(ref MyState state, float delta)
{
    var rigidbody = GetComponent<Rigidbody>(); 
    rigidbody.velocity = newVelocity;
}

```

C#

#### ✅ The Right Way: Caching Components

The solution is simple: find the component once in`Awake()` and store a reference to it (cache it). Then, in your simulation, you can access the cached reference instantly.

##### ✅ A Good Example from Our Project:

`AIMovementModule.cs` needs to know the size of its collider. Instead of calling`GetComponent ()` repeatedly, it gets the collider once in`Awake()` and stores its size in`_cachedColliderSize`.

```
// AI._Module.Movement.AIMovementModule.cs

public class AIMovementModule : PredictedIdentity<AIMovementStateData>
{
    private Vector2 _cachedColliderSize; // The cached value

    private void Awake()
    {
        // ✅ Get the component ONCE at the start.
        var collider = GetComponent<BoxCollider2D>();
        if (collider == null) throw new System.Exception("...");

        // ✅ Store the value for later, fast access.
        _cachedColliderSize = collider.size;
    }
}

```

C#

---

## 🎓 Conclusion

By internalizing these two principles, you will ensure your game runs smoothly even with many objects in the world.

In the future, we’ll explore a more in-depth example of good game architecture alongside more advanced performance optimizations — such as determining acceptable bandwidth limits (KB/s) for different types of games, disabling the synchronization of objects your player can’t see, and replacing heavy full-object physics synchronization with lighter alternatives (e.g., running a raycast on the server and only syncing the result if it hits). These techniques will help you push your game’s performance even further while keeping network usage efficient.

---

### Next Up: Part 7 – Ownership & Animation

Now that your code is fast, let’s tackle one of the most complex architectural challenges: how and when to play animations in a networked environment.

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
