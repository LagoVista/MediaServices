# Session Task 038 — Immutable External Media Delivery

## Status

**COMPLETE — GREEN BUILD + FOCUSED TEST PROOF; READY TO MERGE**

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
Implemented a provider-neutral short-lived external-read lease for one exact immutable media revision. Lease issuance reuses the existing cloud-storage read-URL abstraction, performs tenant/authorization checks before URL creation, binds the lease to the selected immutable revision storage reference, and returns revision-authoritative metadata without exposing storage-provider vocabulary.

### Final external-read contract
- `ImmutableMediaReadLeaseRequest` accepts an optional `LifetimeMinutes`.
- `ImmutableMediaReadLease` returns URL, ValidUntilUtc, MediaResourceId, RevisionId, FileName, ContentType, ContentLength, and ContentSha256.
- Manager entry point: `CreateImmutableMediaReadLeaseAsync(...)`.
- REST entry point: `POST /api/media/resource/{id}/revision/{revisionid}/external-read-lease`.

### Authorization / tenant isolation
Lease issuance loads the media resource, rejects an active-organization mismatch before URL creation, preserves the existing `AuthorizeActions.Read` authorization path, then resolves the requested revision by immutable revision id. Missing resources, missing revisions, and missing revision storage references fail before an external URL is created.

### Storage implementation
No SeaweedFS/Azure/S3 concepts were added to the public MediaServices contract. The implementation extends the existing provider-neutral `IMediaServicesRepo.GetMediaReadUrlAsync(...)` capability with an explicit lifetime and requests a public read-only URL for the exact selected revision storage reference. Returned URLs must be absolute HTTPS URLs.

### Exact-revision / integrity proof
Lease creation resolves `resource.History` by the requested revision id and passes that revision's `StorageReferenceName` to storage. It never resolves through `CurrentRevision`, so later authoring/current-revision changes do not retarget an issued or regenerated lease. File name, MIME type, content length, and SHA-256 are copied from the selected revision metadata.

### Expiry / retry semantics
- Default lifetime: 60 minutes.
- Maximum lifetime: 120 minutes.
- Minimum lifetime: 1 minute.
- Expiry is enforced by the storage-provider signed read URL.
- Revocation is expiry-only; no durable lease state is persisted.
- Callers regenerate a fresh URL from the same immutable media resource id + revision id during retry.

### Tests added
Focused tests cover authorized exact-revision lease issuance, cross-organization rejection before URL creation, missing revision rejection before URL creation, exact-revision targeting when `CurrentRevision` has moved, public read-only URL scope, bounded/default lifetime, HTTPS-only external URL enforcement, revision metadata identity, retry regeneration for the same immutable reference, and storage-provider vocabulary absence from the public DTO.

The authoritative Build Server compiled `LagoVista.MediaServices.MediaTests` successfully as part of the repository build. After the Build Server gained first-class exact-commit .NET test proof, focused proof `7314394a34e544c0877265ab5207701d` ran the actual PR head `d6d48f0cef13bba26dbcfc0bd4788e2121b2cbde` under `feature/campaign-execution-foundation` with filter `FullyQualifiedName~ImmutableMediaRevisionTests`. Result: **17/17 passed** in 1.5590s, including exact-revision targeting, cross-organization rejection before URL creation, missing-revision rejection, bounded lifetime, retry regeneration, HTTPS enforcement, legacy immutable-read compatibility, and provider-vocabulary absence.

### Build proof
- Build Server proof id: `abe7be04d3bc4f38a967daafe9bd8234`
- Source commit: `74da3f92207e2c05f5aaf14c9f3452aa157e0f09`
- Build-normalized proof commit: `b8e49c4...`
- Platform workstream: `feature/campaign-execution-foundation`
- Source branch: `session-038-immutable-external-media-delivery`
- Result: succeeded, 0 warnings, 0 errors.
- Workstream packages: `7.0.23-ws-c-74da3f92`.
- Focused test proof: `7314394a34e544c0877265ab5207701d` — 17/17 passed at PR head `d6d48f0cef13bba26dbcfc0bd4788e2121b2cbde`.
- Lower-layer storage sanity check confirms S3-style public URLs are presigned GETs with requested expiry, while Azure SAS URLs carry Read-only permission and explicit expiry.
- No stable release was published.

### Campaigns / Instagram resume impact
Campaigns can now request a fresh short-lived HTTPS read URL for an exact immutable media revision without persisting the URL and without knowing the underlying storage provider. This provides the MediaServices-side primitive needed for Instagram/Meta-style pull-media execution.
