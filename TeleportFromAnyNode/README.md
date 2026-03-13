# Teleport From Any Node

Lets the player access the teleport feature from any Root Node and not just the Pavilion (hub) Root Node.

Avoids issues that [TeleportFromAnywhere] has at the cost of being able to teleport only from the Root Nodes.

The issue with [TeleportFromAnywhere] is that since it lets the player teleport away from anywhere, it now has to take
into account for situations where it shouldn't let the player do so to avoid breaking the game (e.g. boss battles or the
Prison sequence). While some cases are handled, it still misses some others, for example, teleporting after restoring
power in Outer Warehouse can cause players to nearly miss the first meeting with Yanlao, and teleporting to the Pavilion
near the end of the Prison sequence can skip the scene where Chiyou returns Yi to the Pavilion after collapsing in
Factory Underground. Other edge cases may exist as well.

[TeleportFromAnywhere]: https://thunderstore.io/c/nine-sols/p/Ixrec/TeleportFromAnywhere/
