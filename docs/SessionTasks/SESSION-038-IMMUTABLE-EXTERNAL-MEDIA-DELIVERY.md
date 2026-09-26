# Session Task 038 — Immutable External Media Delivery

## Status

**IMPLEMENTED — PR HANDOFF; STABLE BUILD PROOF REQUIRES MERGE TO MASTER**

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

## Completion Report## Completion Report

### Summary
Implemented a provider-neutral external-read lease for one exact immutable media revision.

The implementation reuses the existing provider-neutral storage read-URL primitive rather than adding a MediaServices proxy route. Issuance stays in MediaServicesManager, where the active organization and normal media read authorization are verified before any external URL is created.

### Final external-read contract
Added ImmutableExternalMediaReadLease with:

- Url
- ValidUntilUtc
- MediaResourceId
- RevisionId
- FileName
- ContentType
- ContentLength
- ContentSha256
- RevocationSupported

Added:

- IMediaServicesManager.CreateImmutableExternalMediaReadLeaseAsync(...)
- authenticated POST /api/media/resource/{id}/revision/{revisionid}/external-read-lease
- optional lifetimeMinutes query parameter

Default lifetime is 30 minutes. Maximum lifetime is 60 minutes.

The public DTO exposes no SeaweedFS, S3, Azure, bucket, container, or storage-reference vocabulary.

### Authorization / tenant isolation
Lease issuance:

1. validates resource and revision identifiers;
2. loads the media resource;
3. rejects active-organization mismatch before URL generation;
4. invokes the existing AuthorizeAsync(..., Read, ...) path;
5. resolves the requested revision from history;
6. generates a URL only for that revision's immutable storage reference.

Cross-organization callers cannot generate a lease.

### Storage implementation
IMediaServicesRepo already exposed provider-neutral read-URL generation backed by ICloudFileStorageClient.CreateReadUrlAsync.

The session adds a lifetime-aware overload:

GetMediaReadUrlAsync(blobReferenceName, org, lifetime, ...)

The existing public/internal scope overloads remain intact. MediaServicesRepo centralizes the implementation and enforces a maximum one-hour URL lifetime.

No storage-provider details are added to the manager or public lease contract.

### Exact-revision / integrity proof
Lease issuance uses the explicitly requested MediaResourceHistory entry and that entry's StorageReferenceName, filename, MIME type, content size, and SHA-256 identity.

Tests deliberately move CurrentRevision to another revision and verify the lease still targets the originally requested revision. No current/latest lookup participates in lease resolution.

The existing immutable byte-read tests remain in the same suite and continue to cover size/hash verification of immutable bytes.

### Expiry / retry semantics
- default lease lifetime: 30 minutes;
- maximum lease lifetime: 60 minutes;
- invalid zero/negative/greater-than-maximum lifetimes are rejected;
- storage-backed URL expiry remains authoritative;
- MediaServices returns its expected ValidUntilUtc;
- revocation is not supported; leases are expiry-only;
- retries regenerate a fresh URL from the same immutable resource/revision reference;
- returned URLs must be absolute HTTPS URLs.

### Tests added
Added focused coverage proving:

- exact requested revision is leased even after CurrentRevision changes;
- revision-authoritative filename/MIME/length/hash are returned;
- cross-organization issuance is rejected before URL creation;
- missing revision does not create a URL;
- retries produce a fresh URL for the same immutable reference;
- lifetime bounds are enforced;
- non-HTTPS URLs are rejected;
- the public DTO contains no storage-provider vocabulary.

The existing immutable revision byte-read tests remain green in the same test project.

### Build proof
Implementation source commit:

- 68cd3b43b7e6f9aee022545cf5813880faf53e3e

Focused edit-workspace validation:

- Build Server operation ebe4694b16b84cac8ffb6889932cd674
- command: dotnet test Tests/LagoVista.MediaServices.MediaTests/LagoVista.MediaServices.MediaTests.csproj --no-restore
- result: succeeded

Authoritative stable build_repository for the feature-branch source commit was correctly rejected because that commit is not reachable from MediaServices:master.

A build attempt using nuviot/platform:feature/campaign-execution-foundation produced Build Server proof id c420a11eca3840da94cf045f8df12b08 and correctly failed PLAT005 because LagoVista/MediaServices is not enrolled in that workstream manifest.

No unrelated workstream was expanded and no package was published.

The final stable exact-commit Build Server proof must therefore be run after this PR is merged to master, using the resulting master commit.

### Campaigns / Instagram resume impact
Once merged and stable-build-proven, Campaigns can request a fresh, short-lived HTTPS URL for an exact immutable media revision during provider execution/retry without persisting the URL or learning storage implementation details.

This supplies the missing provider-neutral pull-media capability identified by Campaigns Session 034.### Campaigns / Instagram resume impact
_TODO_
