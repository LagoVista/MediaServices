# Session Task 038 — Immutable External Media Delivery

## Status

**READY TO START NOW**

Campaigns Session 034 proved that Instagram organic publishing requires Meta to fetch immutable execution media from a publicly reachable URL. Session 028/MediaServices Session 027 provide immutable revision identity and bytes, but no provider-neutral short-lived external-read lease.

## Objective

Add the smallest provider-neutral MediaServices capability that can issue a short-lived read-only external URL/lease for one exact immutable media revision without exposing storage-provider details to Campaigns.

This session belongs to **LagoVista/MediaServices**.

## Repository

- `LagoVista/MediaServices`
- Start from current `master`.

Read first:

- `IMediaServicesManager`
- `GetImmutableMediaRevisionAsync`
- immutable revision tests
- media storage/repository abstractions
- current public media/resource read paths
- Campaigns Session 028 and Session 034 completion reports

## Required semantics

Given:

- media resource id;
- immutable revision id;
- active organization/user context;

issue an external HTTPS read capability bound to that exact revision.

The result must not expose storage implementation vocabulary.

At minimum include:

- URL;
- expiry/valid-until;
- exact media resource id;
- exact revision id;
- content type when authoritative;
- content length/hash identity when available/appropriate.

The URL itself is ephemeral and must not become durable business state.

## Security / authorization

Before issuing a lease:

1. load/authorize the media resource for the active tenant;
2. verify the exact immutable revision exists;
3. reject organization mismatch before lease creation;
4. preserve the same tenant/auth rules as immutable revision read;
5. never expose raw storage credentials.

The lease must be:

- read-only;
- short-lived;
- scoped to the exact immutable object/revision;
- non-listing;
- non-write;
- safe to regenerate on retry.

## Storage abstraction

Do not hard-code SeaweedFS/Azure/S3 vocabulary into the MediaServices public contract.

If the underlying storage interface lacks a safe temporary-read URL primitive:

- add the smallest provider-neutral storage capability needed;
- or document the exact lower-layer gap and stop.

Do not fabricate a permanent public URL.

## Integrity / immutability

A lease must resolve to the same immutable revision identity that `GetImmutableMediaRevisionAsync` would open.

Prove:

- revision id cannot drift to current/latest media;
- hash/size identity remains stable;
- filename/MIME metadata remains revision-authoritative;
- a later authoring revision does not change an already-issued lease target.

If provider-side redirects are used, they must not allow target drift.

## Expiry / retry

Define:

- default lease lifetime;
- bounded maximum lifetime;
- expired lease behavior;
- regeneration from the same immutable reference;
- whether revocation is supported or expiry-only.

Campaigns must be able to request a fresh lease during retry rather than persist the URL.

## Public-route option

If the safest implementation is a MediaServices-controlled signed/tokenized HTTPS endpoint rather than a storage-native signed URL, that is acceptable if:

- the token is unguessable/signed;
- expiry is enforced server-side;
- the route is read-only;
- the token binds exact resource + revision;
- tenant authorization occurs at issuance;
- the URL contains no durable credentials;
- the endpoint does not expose arbitrary storage lookup.

Choose the narrowest robust option.

## Required tests

At minimum prove:

- authorized exact revision gets a lease;
- wrong tenant cannot get a lease;
- missing resource/revision fails;
- lease targets exact revision after parent current revision changes;
- read-only behavior;
- expiry enforcement;
- tampered token/path rejected if using tokenized route;
- content type/length/hash identity matches immutable revision;
- retry can obtain a new URL for the same immutable reference;
- storage-provider details do not leak through the public DTO;
- legacy immutable byte-read path remains green.

## Build / release proof

- run focused MediaServices tests;
- run exact Build Server validation for final source commit;
- record proof id + source/proof commits;
- do not publish/release stable packages unless explicitly requested.

## Guardrails

Do not:

- make media permanently public;
- expose SeaweedFS/Azure/S3 identifiers in the public lease DTO;
- return mutable/current media when a revision id was requested;
- persist access tokens or storage credentials;
- put Campaigns/Instagram-specific concepts in MediaServices;
- publish/release stable packages.

## Definition of done

One of these must be true:

1. MediaServices exposes a provider-neutral short-lived immutable external-read lease with tenant-safe exact-revision behavior and green Build Server proof; or
2. the smallest lower-layer capability gap is precisely documented and the session stops without storage coupling.

Successful completion unblocks the Campaigns execution-media resolver from adding an optional external-read path for Instagram-style pull-media providers.

## Completion Report

### Summary
_TODO_

### Final external-read contract
_TODO_

### Authorization / tenant isolation
_TODO_

### Storage implementation
_TODO_

### Exact-revision / integrity proof
_TODO_

### Expiry / retry semantics
_TODO_

### Tests added
_TODO_

### Build proof
_TODO_

### Campaigns / Instagram resume impact
_TODO_
