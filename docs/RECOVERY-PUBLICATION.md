# Recovery publication hardening

This note supplements [the recovery contract](RECOVERY.md) for 0.4.0-alpha.1.

The browser writer attaches transaction-completion/abort handlers before queuing requests and explicitly aborts after any synchronous exception. Rejecting a JavaScript Promise does not itself abort an IndexedDB transaction. Without this distinction, an exception after a queued manifest write could report failure while the browser later commits the write. The regression suite injects a synchronous failure after that request is queued and verifies that the previous manifest remains byte-for-byte unchanged.

A missing source key aborts publication. Duplicate photo identities, invalid dimensions, inconsistent content lengths, mismatched reference sets and changed source hashes are rejected before publication. Warm commits still use key-only existence checks; they do not reload or rehash already-stored original values. On restore the C# layer verifies the actual original's length and hash.

Transactions request `{ durability: 'strict' }`. This expresses a preference for persistence checks before completion, as defined by the [IndexedDB specification](https://www.w3.org/TR/IndexedDB/#transaction-durability-hint). It is not a guarantee against browser termination, disk failure or power loss, and it does not add a journal or cross-tab conflict resolution.

Late success following a rejected blocked-open request closes the unowned connection instead of leaving it alive. Normal version-change notifications also close the connection. Unreadable recovery remains protected until explicit replacement, and failures preserve the coordinator's previous revision acknowledgement.

Tests exercise actual IndexedDB transactions in isolated browser contexts, including synchronous failure, changed content hashes, duplicate IDs, length mismatch, missing-reference rollback and explicit retry. Diagnostics record the effective durability mode without treating it as a hardware certification.
