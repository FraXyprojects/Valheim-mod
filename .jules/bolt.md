## 2024-05-19 - Expensive FindObjectsByType in Unity Update loops
**Learning:** Found an expensive `UnityEngine.Object.FindObjectsByType` call in a method that is periodically invoked by a Unity `Update` loop. `FindObjectsByType` traverses the entire scene hierarchy which can be extremely slow in large scenes or when there are many objects.
**Action:** Instead of `FindObjectsByType` which searches the entire scene, use Unity's spatial queries like `Physics.OverlapSphere` to query objects within a radius to vastly improve performance.
## 2024-05-19 - Garbage Collection pressure from OverlapSphere and HashSet
**Learning:** While `Physics.OverlapSphere` is much faster than `FindObjectsByType`, allocating new arrays and `HashSet` objects every scan creates Garbage Collection (GC) pressure which can cause stuttering.
**Action:** Use `Physics.OverlapSphereNonAlloc` with a pre-allocated array, and reuse a single cached `HashSet<int>` across scans by clearing it before use.
## 2024-05-19 - Reflection overhead in tight loops
**Learning:** `GetField`, `GetProperty`, and looping through `GetMethods` are heavy Reflection operations. Doing this repeatedly per-frame in `Update` loops (e.g. tracking Valheim character health via `SafeInvoke`) burns significant CPU cycles unnecessarily.
**Action:** Always implement a static `Dictionary` cache (keyed by `Type.FullName` + MemberName + Arguments) to store and reuse `FieldInfo`, `PropertyInfo`, and `MethodInfo` to turn heavy Reflection into O(1) lookups.
## 2024-05-19 - Repetitive String Allocation and Normalization in Event Processing
**Learning:** Found that string normalizations and `.Any()` lookups involving arrays in `Utility/ChronicleFilters.cs` and `Utility/ValheimNames.cs` were dynamically calculating results and allocating arrays (like the tokens array) repeatedly during frequent event checks (like checking progression contexts and milestones).
**Action:** Lift array definitions into static readonly fields. Lift calculation loops (like `.Select(NormalizeKey).ToArray()`) into static constructors for pre-computation, minimizing GC pressure and redundant CPU cycles in frequent operations.
## 2026-09-27 - Caching String Normalizations
**Learning:** Under high concurrency, heavy string manipulations like normalizations (FormD -> Remove marks -> FormC) in C# create significant CPU overhead and GC pressure. When capping the size of a `ConcurrentDictionary` acting as a cache, avoid calling `.Clear()` because it locks all internal buckets, stalling other threads.
**Action:** Use a bounded `ConcurrentDictionary<string, string>` to memoize repetitive string manipulations. To cap its size without locking buckets, use `Interlocked.Exchange(ref _cache, new ConcurrentDictionary<string, string>())` to safely swap in a new empty dictionary reference.
