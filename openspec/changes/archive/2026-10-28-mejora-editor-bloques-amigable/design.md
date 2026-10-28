# Design

## Context

See `proposal.md` for motivation. The editor already represents content as React-free DOM blocks, serializes them to a single `BlocksJson` textarea, and keeps a contextual formatting bubble for selected rich text. The refinement must remain within the existing vanilla JS, Tailwind and sanitization contract.

## Goals / Non-Goals

**Goals:**

- Improve block affordance without introducing a second editing model.
- Keep the existing JSON shape and server-side sanitization intact.
- Provide immediate visual feedback for bold, italic and link state.

**Non-Goals:**

- Replacing the editor with a framework or a third-party block library.
- Changing block persistence, upload endpoints or content limits.
- Reworking the public article/page rendering.

## Decisions

- **Card wrapper instead of a global restyle**: each block keeps its existing DOM body, header toolbar and serialization logic, while the card receives border, radius, background and side controls. This preserves behavior and limits the blast radius.
- **Toolbar type selector includes H2/H3 options**: heading level is moved out of the block body and represented as two selectable heading options. The toolbar icon and label reflect the chosen level; a hidden field keeps `readBlock()` compatible with the existing JSON contract.
- **HTML5 drag and drop on the six-dot handle**: the handle is draggable only, so text selection remains untouched. Drop position is derived from the pointer's half of the target block, then the blocks array is spliced and re-rendered.
- **Format state derived from the current selection**: `queryCommandState` and the selected anchor's `a` ancestor drive `aria-pressed` and `.is-active` in both the block toolbar and the bubble. Applying a command updates the state immediately after `execCommand`.
- **Bubble positioning follows the selected range**: the existing bubble is kept, but its position uses the range rectangle instead of only the field rectangle, reducing the gap between selection and controls.

## Risks / Trade-offs

- **Two toolbars can compete for space** → the block toolbar is hidden while the bubble is open; the bubble exposes the same format actions.
- **Drag and drop can conflict with content selection** → only the handle is draggable, not the whole card.
- **Heading-level migration can break JSON reads** → a hidden `[data-field="level"]` input keeps `readBlock()` unchanged.
- **`execCommand` is legacy API** → it is already used by the editor and remains isolated behind the existing format actions; no new dependency is introduced.
