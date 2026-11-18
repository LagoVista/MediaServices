# Trexel video disclosure policy — v1

**Scope:** Advertisements assembled from `VirtualTeamMember` source entities in MediaServices. Other video compositions are not affected.

**Goal:** Give viewers clear, positive notice that they are viewing an AI-powered virtual team member, while keeping the sole conversion objective a visit to the Trexel's home page. The downstream home-page chat flow is separate.

## Mandatory identity disclosure

The exact phrase **AI Virtual Team Member** must appear visibly in three editable composition-label slots:

1. **Opening** — establish that the featured Trexel is AI, not a human employee.
2. **Presenter** — keep the AI identifier visible for the entire spoken segment, including when clips circulate independently.
3. **Closing** — reinforce AI identity alongside the invitation to visit the Trexel home page.

The presenter script is always `VirtualTeamMember.VideoIntroScript`. The policy does **not** require the presenter to speak the AI disclosure, and it must not substitute `PublicIntro` or the home-page `ResumePageCallToAction`.

## Configurable marketing copy

The closing block has a separate editable value-proposition label, initially:

> Pay for outcomes, not hours worked.

This tagline is marketing copy, **not** the disclosure. It may be revised without changing `VideoIntroScript` or regenerating the presenter. The default video call-to-action is “Meet [AvatarName] on AppTrex.” The clickable link to the canonical Trexel home page is attached to the video placement/player or advertising destination; ordinary burned-in text is not a clickable link.

No specific price, hourly-rate comparison, human-equivalent credentials, or guaranteed outcomes should be asserted without substantiation.

## Technical enforcement

- The `TrexelVideoDisclosurePolicy` code injects four stable editable text-label slots on new or synchronized Trexel compositions.
- The `VideoComposition.VideoDisclosurePolicyVersion` field identifies the policy used and contributes to the composition's input hash.
- Assembly preparation checks the version and mandatory labels on all three **effective** blocks, including referenced block templates.
- Vimeo publishing rechecks the effective labels and requires a current, successfully assembled output that matches those inputs. Editing or removing identity labels after assembly requires a compliant reassembly before publishing.
- Non-Trexel compositions retain their existing behavior.
- The standard layout and closing marketing copy may evolve independently of presenter-video generation.

**Operational process:** Build the Trexel presenter from `VideoIntroScript` → create the three-block composition → inspect disclosure and marketing-copy slots → assemble → review the rendered file → publish and attach the correct Trexel home-page destination.

**Future policy updates:** Increment the policy version when disclosure requirements change; update the default label layout/text and the checks together. Reassemble affected videos before republishing. The composition-template version remains a separate record of visual treatment.

**Legal note:** This is a technical transparency control, not a determination of advertising-law compliance for every market or distribution platform. Review the actual final video and platform settings before broad external distribution.


## One master for the standard three-block composition

**Authoritative resource:** one organization-owned `VideoCompositionTemplate` named
`AppTrex Standard Trexel Video` (suggested key `apptrex-standard-trexel-video`).
The Trexel Video Workbench chooses this resource when a composition is first created.
The Intro, Content, and CallToAction blocks may themselves reference
`VideoCompositionBlockTemplate` scenes for additional reuse.

**V1 editing:** Use the existing Video Production Workbench to edit a working
composition and select **Save as Template → Update existing master**. This
updates the same template record and increments its version. Do not create
a replacement template on every edit; that would lose the stable reference.

**On every Trexel reassembly:** The MediaServices assembly service reloads
the current source `VideoCompositionTemplate`, adopts its blocks, labels,
audio, and backgrounds, retains the previously generated presenter media,
applies the mandatory AI disclosure, and resolves any referenced scenes.
The final effective layout and template version are persisted back to the
composition. Vimeo publication re-evaluates that same master and rejects
outdated renders until reassembled.

**Ownership boundary:** The master owns visual treatment and marketing
copy, including the closing value-proposition label.
The VTM owns the presenter video, `VideoIntroScript`, identity and title.
`VideoComposition.CallToAction` is the *video* home-page invitation, not
the home-page meeting or chat CTA.
Generic (non-Trexel) video compositions retain the existing snapshot rules.

The master must still be designed with actual Intro/Closing media resources
and an appropriate Content background. These are content-design inputs,
not programmatically invented assets. When the master changes, reassemble
linked videos to propagate the new design; no provider narration rerun is needed.
