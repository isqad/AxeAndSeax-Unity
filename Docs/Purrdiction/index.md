# PurrDiction

{% embed url="<https://youtu.be/GcDFfpGypZM>" %}

Client-side prediction is a technique used particularly in games to provide a local, responsive experience in a multiplayer environment. It involves predicting future game states on the client side based on user inputs, allowing the game to feel instant and reactive.

For example, if you are controlling a character and keep moving right, the client can predict that you will continue moving right and display this movement immediately. This way, when you interact with other players, both see a consistent outcome.

Of course, predictions can sometimes be incorrect. When this happens, the client needs to apply corrections. This involves adjusting the game state to match the server's authoritative state, ensuring that everything remains consistent and accurate.

Don't worry if this sounds a bit confusing at first—this overview is meant to set the stage and help you understand the design decisions and limitations involved in client-side prediction.

<figure><img src="https://3317286851-files.gitbook.io/~/files/v0/b/gitbook-x-prod.appspot.com/o/spaces%2FsgxJN58uttGJCEXbBIwp%2Fuploads%2Fgit-blob-aaf0b9928068ddefa22505aad8e1f3f9f5aed232%2F1715578825487.png?alt=media" alt=""><figcaption></figcaption></figure>
