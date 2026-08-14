# AI-Assisted Video Composition Scene Design

## Purpose

This document captures the initial architecture and implementation plan for adding AI-assisted scene design to the MediaServices video composition workflow.

The goal is to let a content builder assemble a base composition and scene using the capabilities already supported by the video composition system, then ask a model to improve the scene layout, pacing, timing, and visual hierarchy.

The model should act as a constrained scene designer, not as an unrestricted content generator. It should use the presenter, images, labels, timing controls, backgrounds, and effects that already exist in the composition system, explain what it recommends and why, and identify additional assets that would materially improve the scene.

This is intentionally a V1 framing document. The first implementation should be allowed to limp along. We should establish the durable intent model and proposal contract first, then tune prompts, schema, heuristics, and UI based on real results.

---

## Current Video Composition Capabilities

The composition system already supports enough primitives to make model-assisted layout useful.

### Blocks

A composition contains ordered blocks. A block can currently represent presenter/video scenes, image scenes, and source-less Content scenes backed by image or looping video backgrounds.

Important block-level capabilities include:

- Duration
- Fade in/out
- Source/presenter video
- Background image or looping video
- Background audio
- Presenter scale
- Presenter horizontal/vertical position
- Overlay images
- Text labels

### Overlay images

Overlay images currently support:

- Existing Media Resource reference
- Scale
- Horizontal position
- Vertical position
- Opacity
- Delay/start time
- Visible duration
- Fade in/out

### Text labels

Text labels currently support:

- Text or composition-field binding
- X/Y position
- Font size
- Bold
- Color
- Alignment
- Maximum width
- Delay/start time
- Visible duration
- Fade in/out
- Text effect: None, Drop Shadow, Glow
- Effect color

### Live preview

The composition workbench now has a block-local scene clock. Presenter/background video playback, scrubbing, labels, and overlay images can be previewed against the same block timeline.

This is important because AI proposals can be previewed immediately without first performing a full FFmpeg assembly.

---

## Product Goal

Given an existing composition and an existing block, the system should be able to send the model:

1. The creative intent for the complete composition.
2. The content intent and design guidance for the selected block.
3. The script/narration associated with the scene when available.
4. The current block configuration.
5. The assets already attached to the block.
6. The exact rendering/layout capabilities supported by the application.
7. Optional additional user guidance for this design attempt.

The model should return a strict structured proposal that contains:

1. A recommended arrangement of existing scene elements.
2. Timing/layout changes using only supported capabilities.
3. A human-readable summary of the recommendation.
4. A rationale explaining why those choices improve the scene.
5. Additional recommendations that may not directly map to mutable properties.
6. Warnings about readability, density, unsuitable assets, pacing, etc.
7. Recommendations for additional assets the scene would benefit from, including what those assets should contain.

The proposed layout should be previewable before it is applied to the persisted composition.

---

## Core Design Principle

The model is a constrained designer operating on a known design surface.

It should not be allowed to invent unsupported rendering capabilities or silently replace the authored content.

For V1:

- The model may rearrange existing presenter, image, and label elements.
- The model may retime existing image and label elements.
- The model may change supported visual properties such as scale, position, color, text effect, alignment, opacity, and fades.
- The model should preserve existing element IDs.
- The model should not invent new Media Resource IDs.
- The model should not delete assets as part of the proposal contract unless we explicitly add that behavior later.
- The model should not rewrite script or label text in the layout proposal unless we explicitly add that capability later.
- The model should not change ownership, block type, entity identity, or unrelated composition metadata.
- The model should be allowed to recommend that no layout change is necessary.

Any additional media that would improve the scene should be returned separately as an asset recommendation, not injected into the block.

---

## Durable Composition-Level Creative Direction

Creative direction should be persisted on the composition so that scene design remains consistent across the full video.

Recommended first model:

```csharp
public class VideoCompositionCreativeDirection
{
    public string VisualStyle { get; set; }
    public string PresenterGuidance { get; set; }
    public string TextGuidance { get; set; }
    public string VisualAssetGuidance { get; set; }
    public string PacingGuidance { get; set; }
    public string AdditionalGuidance { get; set; }
}
```

Recommended property on `VideoComposition`:

```csharp
public VideoCompositionCreativeDirection CreativeDirection { get; set; }
    = new VideoCompositionCreativeDirection();
```

### Suggested UI intent

These fields should eventually live in a collapsible **Creative Direction** section on the composition editor.

They are all optional.

Example values:

**Visual Style**

> Clean, modern, premium, restrained. Avoid looking like a slide deck pasted over video.

**Presenter Guidance**

> Keep the presenter visible most of the time, but reduce prominence when a supporting visual carries the explanation.

**Text Guidance**

> Prefer short phrases and strong hierarchy. Avoid dense paragraphs. Reveal bullet points sequentially when useful.

**Visual Asset Guidance**

> Feature diagrams and product screenshots prominently enough to read. Prefer one major supporting visual at a time.

**Pacing Guidance**

> Change visual emphasis every 6-10 seconds when the narration supports it.

**Additional Guidance**

> Use a polished business-video aesthetic suitable for a non-technical audience.

---

## Durable Block-Level Intent

Each block should carry two additional authored fields:

```csharp
public string ContentIntent { get; set; }
public string DesignGuidance { get; set; }
```

### ContentIntent

Describes what the scene must communicate and which content matters most.

Example:

> Explain the three ways the system compounds business knowledge. Feature the architecture graphic and the three supporting bullet points.

### DesignGuidance

Describes any scene-specific visual direction or constraints.

Example:

> Keep the presenter prominent during the opening and closing. Let the architecture graphic own the middle of the scene. Reveal the bullets sequentially.

This separates **what matters** from **how the scene should feel or behave**.

---

## Scene Design Request

The request contract does not need to mirror the persisted entity exactly. It should be purpose-built for the model.

Suggested shape:

```csharp
public class VideoCompositionBlockDesignRequest
{
    public string DesignCapabilitiesVersion { get; set; } = "1.0";

    public VideoCompositionCreativeDirection CreativeDirection { get; set; }

    public string ContentIntent { get; set; }
    public string DesignGuidance { get; set; }
    public string AdditionalUserGuidance { get; set; }

    public string Script { get; set; }

    public double DurationSeconds { get; set; }

    public VideoCompositionPresenterDesignInput Presenter { get; set; }

    public List<VideoCompositionImageDesignInput> Images { get; set; }
        = new List<VideoCompositionImageDesignInput>();

    public List<VideoCompositionLabelDesignInput> Labels { get; set; }
        = new List<VideoCompositionLabelDesignInput>();

    public VideoCompositionDesignCapabilities Capabilities { get; set; }
}
```

The actual DTO names can change. The important point is to send the model a curated design context rather than serializing the entire entity graph indiscriminately.

---

## Design Capabilities

The model should receive an explicit capabilities description/version so that prompt behavior does not drift away from what the renderer actually supports.

Suggested V1 conceptual capabilities:

```text
Presenter
- scale
- horizontal position
- vertical position

Images
- scale
- horizontal position
- vertical position
- opacity
- start time
- visible duration
- fade in/out

Text
- x/y position
- font size
- bold
- color
- alignment
- maximum width
- start time
- visible duration
- fade in/out
- effect: none, drop shadow, glow
- effect color

Scene
- duration is known
- background may be image or looping video
- block fades exist
```

The prompt should also contain design constraints such as:

- Respect title-safe margins.
- Avoid covering the presenter's face/head region when practical.
- Prefer clear visual hierarchy.
- Avoid displaying too many simultaneous elements.
- Stagger bullet points when that improves pacing.
- Promote a supporting image when the image is carrying the explanation.
- Preserve IDs.
- Return only supported properties.
- Never invent Media Resource IDs.
- Preserve block duration unless a future capability explicitly permits changing it.

Internal concepts such as visual zones may be used in the prompt/model reasoning, but they should not become required author-facing UI.

---

## Scene Design Proposal

The proposal should be richer than a set of coordinates.

Suggested top-level contract:

```csharp
public class VideoCompositionBlockDesignProposal
{
    public bool LayoutChangeRecommended { get; set; }

    public string Summary { get; set; }
    public string DesignRationale { get; set; }

    public List<string> Recommendations { get; set; }
        = new List<string>();

    public List<string> Warnings { get; set; }
        = new List<string>();

    public VideoCompositionPresenterDesign Presenter { get; set; }

    public List<VideoCompositionImageDesign> Images { get; set; }
        = new List<VideoCompositionImageDesign>();

    public List<VideoCompositionLabelDesign> Labels { get; set; }
        = new List<VideoCompositionLabelDesign>();

    public List<VideoCompositionAssetRecommendation> RecommendedAssets { get; set; }
        = new List<VideoCompositionAssetRecommendation>();
}
```

### LayoutChangeRecommended

The model must be allowed to say that the current layout is already appropriate.

This prevents the model from changing a scene merely because it was asked to review it.

### Summary

Short explanation of the overall proposed design.

Example:

> Use the presenter as the visual anchor for the opening, shift emphasis to the architecture graphic during the core explanation, then return the presenter to prominence for the closing statement.

### DesignRationale

Longer explanation of why the proposed changes improve the scene.

Example:

> The presenter currently carries the visual focus for nearly the entire 45-second scene. Breaking the block into distinct visual beats should improve pacing, reduce presenter fatigue, and let the supporting concepts carry more of the explanation.

### Recommendations

Human-readable recommendations that may or may not map directly to mutable block fields.

Example:

- Reduce presenter scale while the architecture graphic is visible.
- Reveal bullet points sequentially rather than simultaneously.
- Return the presenter to stronger prominence for the final statement.

### Warnings

Potential quality problems the model detects.

Example:

- Three bullet points contain too much text to remain comfortably readable at 1080p.
- The supplied diagram is very dense and may need a simplified variant.

---

## Presenter Proposal

Suggested output shape:

```csharp
public class VideoCompositionPresenterDesign
{
    public double Scale { get; set; }
    public double PositionX { get; set; }
    public double PositionY { get; set; }
}
```

For V1 this represents the block's presenter placement. If we later support presenter movement over time, that should be introduced as a separate capability/version rather than overloading this object.

---

## Image Proposal

The model should only reference images already supplied to the block.

Suggested shape:

```csharp
public class VideoCompositionImageDesign
{
    public string ImageId { get; set; }

    public double Scale { get; set; }
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public double Opacity { get; set; }

    public double DelaySeconds { get; set; }
    public double? VisibleDurationSeconds { get; set; }

    public double FadeInSeconds { get; set; }
    public double FadeOutSeconds { get; set; }
}
```

The service applying a proposal should validate that every returned `ImageId` exists on the source block.

---

## Label Proposal

The model should only reference labels already supplied to the block.

Suggested shape:

```csharp
public class VideoCompositionLabelDesign
{
    public string LabelId { get; set; }

    public int X { get; set; }
    public int Y { get; set; }
    public int FontSize { get; set; }
    public bool Bold { get; set; }

    public string Color { get; set; }
    public VideoCompositionTextEffect Effect { get; set; }
    public string EffectColor { get; set; }

    public VideoCompositionTextAlignment Alignment { get; set; }
    public int? MaxWidth { get; set; }

    public double DelaySeconds { get; set; }
    public double? VisibleDurationSeconds { get; set; }

    public double FadeInSeconds { get; set; }
    public double FadeOutSeconds { get; set; }
}
```

For V1 the proposal should not include replacement text. The model may comment that text is too long in `Warnings` or `Recommendations`, but should not rewrite authored copy as part of the layout proposal.

---

## Recommended Asset Contract

Asset recommendations should be first-class from the beginning even if V1 only displays them.

Suggested shape:

```csharp
public class VideoCompositionAssetRecommendation
{
    public string Key { get; set; }

    public string AssetType { get; set; }

    public string Purpose { get; set; }

    public string Description { get; set; }

    public string SuggestedGenerationPrompt { get; set; }

    public double? SuggestedStartSeconds { get; set; }
    public double? SuggestedDurationSeconds { get; set; }

    public string Priority { get; set; }
}
```

Example model response:

```json
{
  "key": "knowledge-compounding",
  "assetType": "image",
  "purpose": "Break up a presenter-heavy section and visualize compounding value",
  "description": "A clean business graphic showing completed deliverables accumulating into a shared organizational knowledge base.",
  "suggestedGenerationPrompt": "Professional editorial illustration showing business deliverables flowing into a shared organizational knowledge base, clean modern enterprise visual style, restrained palette, 16:9 composition.",
  "suggestedStartSeconds": 18,
  "suggestedDurationSeconds": 7,
  "priority": "high"
}
```

V1 does not need to generate or search for the asset.

Future UI could evolve naturally into:

```text
Recommended Asset
Knowledge Compounding Graphic

[ Generate ] [ Find Existing ] [ Ignore ]
```

---

## Structured Model Execution

MediaServices should follow the same general structured-model pattern used elsewhere in the platform:

```text
build deterministic request/context
        ↓
build prompt
        ↓
call structured model with exact response schema
        ↓
normalize response
        ↓
validate response against source block and capabilities
        ↓
return proposal
```

The implementation should not rely on parsing prose into coordinates.

The model should be explicitly instructed to return the exact response schema.

Suggested conceptual service flow:

```csharp
BuildSceneDesignRequest(...)
BuildPrompt(...)
CallStructuredModel<VideoCompositionBlockDesignProposal>(...)
NormalizeProposal(...)
ValidateProposal(...)
ReturnProposal(...)
```

The actual LLM abstraction should reuse existing platform infrastructure where practical rather than introducing a new provider-specific client into MediaServices.

---

## Proposal Validation

The proposal must be treated as untrusted structured input even when the model successfully follows the schema.

Validation should include at least:

- Returned image IDs exist on the source block.
- Returned label IDs exist on the source block.
- No duplicate IDs in proposal arrays.
- Presenter scale is within supported bounds.
- Presenter positions are within 0-1.
- Image scale is positive and within supported bounds.
- Image positions are within 0-1.
- Image opacity is within 0-1.
- Label coordinates remain within the 1920x1080 design surface or accepted safe bounds.
- Font size is reasonable.
- Colors are valid six-digit hexadecimal colors.
- Effect values are supported.
- Delay/duration/fade values are non-negative and do not obviously exceed the block duration.
- Proposal does not reference capabilities outside the requested capability version.

Normalization may clamp minor numeric errors where appropriate, but meaningful schema/content violations should be surfaced rather than silently transformed into a different design.

---

## Apply Semantics

The model should not immediately mutate the persisted composition.

Target workflow:

```text
Current Scene
    ↓
Design This Scene
    ↓
Optional attempt-specific guidance
    ↓
AI Proposal
    ↓
Preview proposed scene
    ↓
[ Apply Design ] [ Try Again ] [ Cancel ]
```

The proposal can be applied to an in-memory clone/current client-side representation for preview.

Only **Apply Design** should copy the proposed mutable fields into the actual block that will eventually be saved.

This makes experimentation inexpensive and protects the canonical composition from direct model writes.

---

## Initial UI Direction

### Composition editor

Add a collapsible **Creative Direction** section containing the persisted composition-level guidance fields.

### Block editor

Add a small **Design Intent** section containing:

- Content Intent
- Design Guidance

Later add a **Design This Scene** action.

### Proposal surface

The first proposal UI can be simple. It should display:

- Summary
- Design rationale
- Recommendations
- Warnings
- Recommended assets
- Apply Design
- Try Again
- Cancel

The existing live preview should show the proposed positions and timing.

The V1 proposal UI does not need a sophisticated diff viewer.

---

## Prompt Guidance

The system prompt/instructions for the design model should make the role explicit.

Conceptually:

> You are a professional video scene designer. Improve the visual composition, hierarchy, and pacing of the supplied scene using only the capabilities and existing elements provided. Do not invent media resources or unsupported effects. Preserve element IDs. Prefer clear hierarchy, readable text, intentional pacing, and visual variety. Explain the design rationale and identify additional assets that would materially improve the scene.

The prompt should provide the composition-level creative direction before the block-level guidance so the model treats the composition direction as the durable visual language.

The model should receive enough semantic information about each asset to understand its role. Media Resource IDs alone are not useful design context.

Where possible, input image descriptions/names and label text should be included.

Script/narration should be included because it gives the model semantic cues for when visual emphasis should change.

Transcript/word-level timing would be valuable later but is not required for V1.

---

## Internal Layout Reasoning

The prompt may describe visual regions/zones internally to help the model reason consistently, for example:

- upper-left
- upper-center
- upper-right
- middle-left
- center
- middle-right
- lower-left
- lower-center
- lower-right

This is an internal prompting technique, not an author-facing product concept.

The final proposal should still use the exact normalized coordinates/pixel coordinates expected by the composition model.

---

## V1 Scope

The first useful implementation should do the following:

1. Add persisted composition-level creative direction.
2. Add persisted block-level Content Intent and Design Guidance.
3. Define request/proposal DTOs.
4. Define a versioned design-capabilities description.
5. Build one structured scene-design executor/service.
6. Send composition guidance, block guidance, script, current layout, existing labels/images, and supported capabilities to the model.
7. Receive an exact-schema proposal.
8. Validate and normalize the proposal.
9. Return rationale, recommendations, warnings, proposed layout, and recommended assets.
10. Allow the workbench to preview the proposal.
11. Allow the user to apply or cancel the proposal.

The first prompt does not need to be perfect.

The first proposal UI does not need to be beautiful.

The first model results do not need to be consistently excellent.

The goal is to establish the complete loop and then tune using real scenes.

---

## Explicit V1 Non-Goals

Do not block V1 on the following:

- Automatic image generation
- Automatic media-library search
- Rewriting scripts
- Rewriting label text
- Composition-wide automatic redesign
- Word-level transcript synchronization
- Presenter movement/keyframes within a block
- New renderer capabilities invented solely for AI design
- Animated image motion
- Arbitrary transitions beyond capabilities already supported
- Full undo/redo history for proposals
- Sophisticated visual diff UI
- Automatic persistence of model proposals

These can be layered on after the core proposal loop is useful.

---

## Future Directions to Preserve

The V1 contracts should leave room for the following without requiring them now.

### Improve Entire Composition

Composition-level creative direction can eventually drive a complete composition review so multiple blocks maintain a consistent visual language and pacing strategy.

### Asset fulfillment

Recommended assets can eventually feed workflows to:

- Generate an image
- Search existing Media Resources
- Ask a user to supply an asset
- Accept/ignore the recommendation

### Transcript-aware scene design

Word/sentence timestamps can eventually let the model align supporting visuals precisely with spoken concepts.

### Richer visual capabilities

Future capability versions may add:

- Presenter movement/keyframes
- Image movement/zoom/pan
- More text effects
- Transitions
- Shapes/callouts
- Multiple presenter layouts
- Dynamic background treatments

These should be added through capability versioning rather than silently changing what the model is assumed to know.

### Quality/review loop

The proposal rationale and warnings could eventually support a separate quality-review model or heuristic scorer.

---

## Recommended Implementation Order

### Phase 1: Persist intent

Backend:

- Add `VideoCompositionCreativeDirection`.
- Add `CreativeDirection` to `VideoComposition`.
- Add `ContentIntent` and `DesignGuidance` to `VideoCompositionBlock`.
- Include these fields in composition input hashing/currentness where appropriate.
- Ensure template cloning preserves creative direction/block intent where desired.

Frontend:

- Add composition Creative Direction UI.
- Add block Design Intent UI.
- Save/load through generated contracts.

### Phase 2: Define AI contracts

Add request/proposal DTOs and proposal validation.

Do this before writing a large prompt so the response schema drives the prompt rather than the other way around.

### Phase 3: Structured executor

Implement the model call using existing structured-output infrastructure.

Initial endpoint/service can simply return a proposal for a selected block.

Suggested conceptual endpoint:

```text
POST /api/media/videocomposition/{compositionId}/block/{blockId}/design
```

Request may contain attempt-specific user guidance.

The server should load the persisted composition/block itself rather than trusting the client to provide canonical entity state.

### Phase 4: Proposal UI and live preview

- Display rationale/recommendations/warnings/assets.
- Apply proposal to a temporary/in-memory block representation.
- Use existing scene preview to visualize proposed timing/layout.
- Add Apply Design / Try Again / Cancel.

### Phase 5: Tune with real content

Test against actual presenter-heavy scenes and iterate on:

- Prompt wording
- Safe-area guidance
- Visual hierarchy rules
- Timing heuristics
- Model choice
- Output normalization
- Recommended asset quality

---

## Suggested First Acceptance Scenario

Use a real 40-60 second presenter-heavy content block containing:

- Presenter video
- Background
- 2-4 overlay images
- 3-5 bullet/text labels
- Existing timing values
- Composition creative direction
- Block content intent and design guidance

Ask the model to design the scene.

A successful first implementation should demonstrate that:

1. The response is mechanically valid.
2. All returned element IDs map to existing elements.
3. The model returns a useful explanation of what it changed and why.
4. The model identifies at least plausible missing assets when the supplied scene lacks visual variety.
5. The proposed coordinates/timing can be shown in the existing live preview.
6. Applying the proposal updates only supported layout/timing fields.
7. Cancelling leaves the persisted block unchanged.

Visual perfection is not the first acceptance criterion. A complete, inspectable, tunable loop is.

---

## Guiding Architecture

```text
Composition Creative Direction
            +
Block Content Intent
            +
Block Design Guidance
            +
Script / Narration
            +
Existing Presenter / Images / Labels
            +
Versioned Rendering Capabilities
            ↓
Structured Scene Design Executor
            ↓
Validated Design Proposal
      /         |          \
 Layout     Rationale    Missing Assets
      \         |          /
            Live Preview
                ↓
       Apply / Retry / Cancel
```

The model proposes. MediaServices validates. The workbench previews. The human decides. The existing renderer remains deterministic.

---

## Session Handoff

The next implementation session should begin with **Phase 1: Persist intent**.

Do not start with prompt tuning or model calls yet.

Establish the composition and block intent fields first, wire them through the existing entity/forms/generated contracts, and make sure they save/load correctly. Once that compiles and the workbench can author them, move to the proposal contracts and structured executor.
