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
