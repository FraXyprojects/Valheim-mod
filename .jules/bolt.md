## 2024-05-19 - Expensive FindObjectsByType in Unity Update loops
**Learning:** Found an expensive `UnityEngine.Object.FindObjectsByType` call in a method that is periodically invoked by a Unity `Update` loop. `FindObjectsByType` traverses the entire scene hierarchy which can be extremely slow in large scenes or when there are many objects.
**Action:** Instead of `FindObjectsByType` which searches the entire scene, use Unity's spatial queries like `Physics.OverlapSphere` to query objects within a radius to vastly improve performance.
## 2024-05-19 - Garbage Collection pressure from OverlapSphere and HashSet
**Learning:** While `Physics.OverlapSphere` is much faster than `FindObjectsByType`, allocating new arrays and `HashSet` objects every scan creates Garbage Collection (GC) pressure which can cause stuttering.
**Action:** Use `Physics.OverlapSphereNonAlloc` with a pre-allocated array, and reuse a single cached `HashSet<int>` across scans by clearing it before use.

## 2026-09-10 - String allocation GC pressure in caching loops
**Learning:** Using string interpolation (`$"{type.FullName}.{memberName}"`) as keys for caching dictionaries creates new string objects on the heap during every lookup. If these lookups occur in frequent paths (like Unity Update loops), the constant string allocations will generate severe GC pressure, negating some of the performance benefits of the cache itself.
**Action:** For caching mechanisms accessed frequently, use a custom struct implementing `IEquatable<T>` as the dictionary key to completely eliminate heap allocations during lookups.
