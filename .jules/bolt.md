## 2024-05-19 - Expensive FindObjectsByType in Unity Update loops
**Learning:** Found an expensive `UnityEngine.Object.FindObjectsByType` call in a method that is periodically invoked by a Unity `Update` loop. `FindObjectsByType` traverses the entire scene hierarchy which can be extremely slow in large scenes or when there are many objects.
**Action:** Instead of `FindObjectsByType` which searches the entire scene, use Unity's spatial queries like `Physics.OverlapSphere` to query objects within a radius to vastly improve performance.
## 2024-05-19 - Garbage Collection pressure from OverlapSphere and HashSet
**Learning:** While `Physics.OverlapSphere` is much faster than `FindObjectsByType`, allocating new arrays and `HashSet` objects every scan creates Garbage Collection (GC) pressure which can cause stuttering.
**Action:** Use `Physics.OverlapSphereNonAlloc` with a pre-allocated array, and reuse a single cached `HashSet<int>` across scans by clearing it before use.
## 2024-05-19 - Repetitive String Allocation and Normalization in Event Processing
**Learning:** Found that string normalizations and `.Any()` lookups involving arrays in `Utility/ChronicleFilters.cs` and `Utility/ValheimNames.cs` were dynamically calculating results and allocating arrays (like the tokens array) repeatedly during frequent event checks (like checking progression contexts and milestones).
**Action:** Lift array definitions into static readonly fields. Lift calculation loops (like `.Select(NormalizeKey).ToArray()`) into static constructors for pre-computation, minimizing GC pressure and redundant CPU cycles in frequent operations.
